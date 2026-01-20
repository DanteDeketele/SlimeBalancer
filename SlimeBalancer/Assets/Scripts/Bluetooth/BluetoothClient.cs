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

    [Header("Options")]
    [Tooltip("Enable extra logging to diagnose issues on specific machines.")]
    public bool logVerbose = true;
    [Tooltip("Baud rate used to open the serial port.")]
    public int baudRate = 115200;
    [Tooltip("Toggle DTR/RTS. Some machines/drivers behave differently. Try disabling if connection fails.")]
    public bool useDtrRts = true;
    [Tooltip("Seconds to wait after opening the port to allow MCU reboot over DTR/RTS.")]
    public float postOpenResetDelaySec = 2.0f;
    [Tooltip("Seconds to listen for handshake text after the initial reset delay.")]
    public float handshakeListenWindowSec = 5.0f;
    [Tooltip("Filters available ports to only those that can be opened briefly. Helps hide stale Bluetooth COM ports.")]
    public bool filterToOpenablePorts = true;
    [Tooltip("Timeout in ms used by the quick open-check when filtering ports.")]
    public int openCheckTimeoutMs = 300;

    [Header("Discovery")]
    [Tooltip("Last seen available COM ports. Useful for manual selection in UI.")]
    public string[] availablePorts = new string[0];

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

    public void ConnectToPort(string portName)
    {
        if (IsConnected || string.IsNullOrEmpty(portName)) return;
        statusMessage = $"Trying port {portName}...";
        if (logVerbose) Debug.Log($"[BT] Manual connect to {portName}");
        scanTokenSource?.Cancel();
        isScanning = false;
        Task.Run(() =>
        {
            try
            {
                CheckSinglePort(portName, CancellationToken.None);
                if (!IsConnected)
                {
                    statusMessage = $"No handshake on {portName}";
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BT] Manual connect error on {portName}: {ex.GetType().Name}: {ex.Message}");
            }
        });
    }

    public void RefreshPorts()
    {
        // Non-blocking refresh
        Task.Run(() =>
        {
            string[] fresh = GetFilteredPorts();
            availablePorts = fresh;
            if (logVerbose) Debug.Log($"[BT] Ports refreshed: {string.Join(", ", fresh)}");
        });
    }

    private string[] GetFilteredPorts()
    {
        string[] ports = SerialPort.GetPortNames();
        Array.Sort(ports, StringComparer.OrdinalIgnoreCase);
        if (!filterToOpenablePorts) return ports;

        List<string> result = new List<string>();
        foreach (var port in ports)
        {
            SerialPort p = null;
            try
            {
                p = new SerialPort(port, baudRate)
                {
                    ReadTimeout = openCheckTimeoutMs,
                    WriteTimeout = openCheckTimeoutMs,
                    DtrEnable = false,
                    RtsEnable = false
                };
                p.Open();
                // Small sleep to emulate minimal activity
                Thread.Sleep(10);
                result.Add(port);
            }
            catch (UnauthorizedAccessException)
            {
                // Port is busy but exists; still list it
                result.Add(port);
            }
            catch (System.IO.IOException io)
            {
                // Likely stale/non-existent; skip
                if (logVerbose) Debug.Log($"[BT] Skipping non-openable port {port}: {io.Message}");
            }
            catch (Exception ex)
            {
                if (logVerbose) Debug.Log($"[BT] Skipping port {port}: {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                try { if (p != null && p.IsOpen) p.Close(); } catch { }
            }
        }
        return result.ToArray();
    }

    private async Task ScanAllPortsParallel()
    {
        string[] ports = GetFilteredPorts();
        availablePorts = ports;
        if (logVerbose) Debug.Log($"[BT] Found ports: {string.Join(", ", ports)}");

        if (ports.Length == 0)
        {
            if (logVerbose) Debug.LogWarning("[BT] No COM ports found. Ensure the device is paired and a Serial Port (SPP) is created in Windows.");
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
            // Expected when one task cancels the others
            if (logVerbose) Debug.Log("[BT] Scan tasks canceled after a successful connection.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[BT] Unexpected scan error: {ex.GetType().Name}: {ex.Message}");
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

            if (logVerbose) Debug.Log($"[BT] Testing {port} @ {baudRate}bps (DTR/RTS={(useDtrRts ? "on" : "off")})");
            testPort = new SerialPort(port, baudRate)
            {
                ReadTimeout = 2000,
                WriteTimeout = 1000,
                DtrEnable = useDtrRts,
                RtsEnable = useDtrRts
            };

            try
            {
                testPort.Open();
            }
            catch (UnauthorizedAccessException ua)
            {
                if (logVerbose) Debug.LogWarning($"[BT] {port} access denied: {ua.Message}");
                return;
            }
            catch (System.IO.IOException io)
            {
                if (logVerbose) Debug.LogWarning($"[BT] {port} I/O error: {io.Message}");
                return;
            }
            catch (Exception ex)
            {
                if (logVerbose) Debug.LogWarning($"[BT] {port} open failed: {ex.GetType().Name}: {ex.Message}");
                return;
            }

            // 1. Wait for potential ESP32 Reboot (essential)
            // We use small sleeps to allow early cancellation
            int resetMillis = Mathf.RoundToInt(Mathf.Max(0f, postOpenResetDelaySec) * 1000f);
            int waited = 0;
            while (waited < resetMillis)
            {
                if (token.IsCancellationRequested) { try { testPort.Close(); } catch { } return; }
                Thread.Sleep(100);
                waited += 100;
            }

            // 2. Listen for Handshake
            string accumulated = "";
            DateTime timeout = DateTime.Now.AddSeconds(Mathf.Max(0.5f, handshakeListenWindowSec));

            while (DateTime.Now < timeout)
            {
                if (token.IsCancellationRequested) { try { testPort.Close(); } catch { } return; }

                try
                {
                    string chunk = testPort.ReadExisting();
                    if (!string.IsNullOrEmpty(chunk))
                    {
                        accumulated += chunk;
                        if (logVerbose) Debug.Log($"[BT] {port} RX: {chunk.Replace("\n", "\\n").Replace("\r", "\\r")}" );
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
                catch (TimeoutException) { }
                catch (Exception ex)
                {
                    if (logVerbose) Debug.LogWarning($"[BT] {port} read error: {ex.GetType().Name}: {ex.Message}");
                }
                Thread.Sleep(50);
            }

            if (logVerbose) Debug.Log($"[BT] {port} no handshake within window.");
            try { testPort.Close(); } catch { }
        }
        catch (Exception ex)
        {
            if (logVerbose) Debug.LogWarning($"[BT] CheckSinglePort fatal on {port}: {ex.GetType().Name}: {ex.Message}");
            try { if (testPort != null && testPort.IsOpen) testPort.Close(); } catch { }
        }
    }

    private void ConnectSuccess(SerialPort port, string portName)
    {
        // This lock ensures only one thread claims victory
        lock (this)
        {
            if (IsConnected)
            {
                try { port.Close(); } catch { } // Another thread beat us by a millisecond
                return;
            }

            IsConnected = true;
            isScanning = false;
            serialPort = port; // Adopt the open port
            connectedPort = portName;
            statusMessage = $"Connected: {portName}";

            // Cancel other searches
            try { scanTokenSource?.Cancel(); } catch { }

            Debug.Log($"[BT] SUCCESS! Connected to {portName}");

            // Start the permanent read thread
            keepReading = true;
            readThread = new Thread(ReadDataThread) { IsBackground = true };
            readThread.Start();
        }
    }

    private void RetryScan()
    {
        isScanning = false;
        statusMessage = "Device not found. Retrying...";
        if (logVerbose) Debug.Log("[BT] Scan failed. Retrying in 3s...");
        Thread.Sleep(3000);
        StartAutoConnection();
    }

    public void Disconnect()
    {
        try { scanTokenSource?.Cancel(); } catch { }
        isScanning = false;
        keepReading = false;
        IsConnected = false;

        try { if (readThread != null && readThread.IsAlive) readThread.Join(500); } catch { }
        try { if (serialPort != null && serialPort.IsOpen) serialPort.Close(); } catch { }

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