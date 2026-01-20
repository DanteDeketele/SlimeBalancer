using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks; // Required for Parallel tasks
using UnityEngine;
using System.Collections;

public class BluetoothClient : MonoBehaviour
{
    [Header("Connection Status")]
    public string connectedPort = "None";
    public bool IsConnected { get; private set; } = false;
    public string statusMessage = "Disconnected";

    [Header("Live Data")]
    public float pitch;
    public float roll;
    public float temperature;

    // --- Internal ---
    private SerialPort serialPort;
    private Thread readThread;
    private bool keepReading = false;
    private bool isScanning = false;
    private string buffer = "";

    // Cancellation token to stop all other searchers once one finds the device
    private CancellationTokenSource scanTokenSource;

    public enum BoardSide { All = 0, Top = 4, Right = 3, Bottom = 2, Left = 1 }

    private void Start()
    {
        StartAutoConnection();
    }

    public IEnumerator Blink(Color color, int count, float delayBetweenBlinks, BoardSide side, Color endColor, InputManager.LightingEffect endEffect)
    {
        for (int i = 0; i < count; i++)
        {
            GameManager.InputManager.SetLightingEffect(
                InputManager.LightingEffect.Custom, Color.black, side);

            yield return new WaitForSeconds(delayBetweenBlinks);

            GameManager.InputManager.SetLightingEffect(
                InputManager.LightingEffect.Custom, color, side);

            yield return new WaitForSeconds(delayBetweenBlinks);
        }

        if (endEffect != InputManager.LightingEffect.Custom)
            GameManager.InputManager.SetLightingEffect(endEffect);
        else
            GameManager.InputManager.SetLightingEffect(InputManager.LightingEffect.Custom, endColor, side);
    }

    public void StartAutoConnection()
    {
        if (isScanning || IsConnected) return;

        isScanning = true;
        statusMessage = "Scanning all ports...";

        // Run the scanner in a background task so Unity doesn't freeze
        Task.Run(() => ScanAllPortsParallel());
    }

    private async Task ScanAllPortsParallel()
    {
        string[] ports = SerialPort.GetPortNames();
        Debug.Log($"[BT] Found ports: {string.Join(", ", ports)}");

        if (ports.Length == 0)
        {
            RetryScan();
            return;
        }

        scanTokenSource = new CancellationTokenSource();
        var token = scanTokenSource.Token;
        List<Task> portTasks = new List<Task>();

        foreach (string port in ports)
        {
            // Launch a separate task for EACH port
            Task t = Task.Run(() => CheckSinglePort(port, token), token);
            portTasks.Add(t);
        }

        try
        {
            // Wait for all to finish (or for one to succeed and cancel the rest)
            await Task.WhenAll(portTasks);
        }
        catch (OperationCanceledException)
        {
            // This is expected when one task cancels the others
        }

        if (!IsConnected)
        {
            RetryScan();
        }
    }

    private void CheckSinglePort(string port, CancellationToken token)
    {
        SerialPort testPort = null;
        try
        {
            if (token.IsCancellationRequested) return;

            testPort = new SerialPort(port, 115200);
            testPort.ReadTimeout = 2000;
            testPort.WriteTimeout = 1000;
            testPort.DtrEnable = true;
            testPort.RtsEnable = true;

            testPort.Open();

            // 1. Wait for potential ESP32 Reboot (essential)
            // We use SpinWait or sleep in chunks to allow early cancellation
            for (int i = 0; i < 20; i++)
            {
                if (token.IsCancellationRequested) { testPort.Close(); return; }
                Thread.Sleep(100);
            }

            // 2. Listen for Handshake
            string accumulated = "";
            DateTime timeout = DateTime.Now.AddSeconds(2.5); // 2.5s listening window

            while (DateTime.Now < timeout)
            {
                if (token.IsCancellationRequested) { testPort.Close(); return; }

                try
                {
                    string chunk = testPort.ReadExisting();
                    if (!string.IsNullOrEmpty(chunk))
                    {
                        accumulated += chunk;
                        // Check for signature
                        if (accumulated.Contains("Mpu_Values") || accumulated.Contains(">>"))
                        {
                            // --- WINNER FOUND ---
                            if (!IsConnected) // Double check to ensure only one winner
                            {
                                ConnectSuccess(testPort, port);
                            }
                            return;
                        }
                    }
                }
                catch { }
                Thread.Sleep(50);
            }

            // If we get here, this port failed.
            testPort.Close();
        }
        catch
        {
            if (testPort != null && testPort.IsOpen) testPort.Close();
        }
    }

    private void ConnectSuccess(SerialPort port, string portName)
    {
        // This lock ensures only one thread claims victory
        lock (this)
        {
            if (IsConnected)
            {
                port.Close(); // Another thread beat us by a millisecond
                return;
            }

            IsConnected = true;
            isScanning = false;
            serialPort = port; // Adopt the open port
            connectedPort = portName;
            statusMessage = $"Connected: {portName}";

            // Cancel other searches
            scanTokenSource.Cancel();

            Debug.Log($"[BT] SUCCESS! Connected to {portName}");

            // Start the permanent read thread
            keepReading = true;
            readThread = new Thread(ReadDataThread);
            readThread.Start();
        }
    }

    private void RetryScan()
    {
        isScanning = false;
        statusMessage = "Device not found. Retrying...";
        Debug.Log("[BT] Scan failed. Retrying in 3s...");
        Thread.Sleep(3000);
        StartAutoConnection();
    }

    public void Disconnect()
    {
        scanTokenSource?.Cancel();
        isScanning = false;
        keepReading = false;
        IsConnected = false;

        if (readThread != null && readThread.IsAlive) readThread.Join(500);
        if (serialPort != null && serialPort.IsOpen) serialPort.Close();

        statusMessage = "Disconnected";
        Debug.Log("[BT] Disconnected");
    }

    // --- Sending Methods (Unchanged) ---
    private void SendString(string message) { if (IsConnected && serialPort?.IsOpen == true) try { serialPort.WriteLine(message); } catch { } }
    public void SendOff() => SendString("Off>>");
    public void SendRainbow() => SendString("Rainbow>>");
    public void SendIdle() => SendString("Idle>>");
    public void SendColor(Color color, BoardSide side)
    {
        int r = Mathf.RoundToInt(color.r * 255);
        int g = Mathf.RoundToInt(color.g * 255);
        int b = Mathf.RoundToInt(color.b * 255);
        SendString($"R: {r}, G: {g}, B: {b}, Side: {(int)side}>>");
    }

    // --- Receiving Logic (Unchanged) ---
    private void ReadDataThread()
    {
        while (keepReading && serialPort != null && serialPort.IsOpen)
        {
            try
            {
                string incoming = serialPort.ReadExisting();
                if (!string.IsNullOrEmpty(incoming)) ProcessIncomingData(incoming);
            }
            catch { Disconnect(); }
            Thread.Sleep(10);
        }
    }

    private void ProcessIncomingData(string newData)
    {
        buffer += newData;
        int terminatorIndex;
        while ((terminatorIndex = buffer.IndexOf(">>")) != -1)
        {
            string cleanMessage = buffer.Substring(0, terminatorIndex).Trim();
            buffer = buffer.Substring(terminatorIndex + 2);
            ParseMessage(cleanMessage);
        }
    }

    private void ParseMessage(string msg)
    {
        if (msg.StartsWith("Mpu_Values:"))
        {
            try
            {
                pitch = ExtractValue(msg, "P: ", ",");
                roll = ExtractValue(msg, "R: ", ",");
                temperature = ExtractValue(msg, "T: ", "");
            }
            catch { }
        }
    }

    private float ExtractValue(string source, string key, string endChar)
    {
        int startIndex = source.IndexOf(key);
        if (startIndex == -1) return 0f;
        startIndex += key.Length;
        int endIndex = string.IsNullOrEmpty(endChar) ? source.Length : source.IndexOf(endChar, startIndex);
        if (endIndex == -1) endIndex = source.Length;
        return float.TryParse(source.Substring(startIndex, endIndex - startIndex).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float r) ? r : 0f;
    }

    private void OnDestroy() => Disconnect();
    private void OnApplicationQuit() => Disconnect();
}