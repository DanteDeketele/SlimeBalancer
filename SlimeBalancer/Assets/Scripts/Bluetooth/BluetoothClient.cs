using UnityEngine;
using System.IO.Ports;
using System.Threading;
using System;
using System.Globalization;

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

    // Internal Threading & Serial
    private SerialPort serialPort;
    private Thread connectionThread;
    private Thread readThread;
    private bool keepReading = false;
    private bool isScanning = false;
    private string buffer = "";

    public enum BoardSide { All = 0, Top = 1, Right = 2, Bottom = 3, Left = 4 }

    private void Start()
    {
        // Start the scanning process in a background thread to keep the game smooth
        StartAutoConnection();
    }

    public void StartAutoConnection()
    {
        if (isScanning || IsConnected) return;

        isScanning = true;
        statusMessage = "Scanning ports...";
        connectionThread = new Thread(ScanAndConnect);
        connectionThread.Start();
    }

    private void ScanAndConnect()
    {
        string[] ports = SerialPort.GetPortNames();
        Debug.Log($"[BT] Found ports: {string.Join(", ", ports)}");

        foreach (string port in ports)
        {
            if (!isScanning) break; // Stop if requested

            Debug.Log($"[BT] Testing {port}...");
            SerialPort testPort = null;

            try
            {
                testPort = new SerialPort(port, 115200);
                testPort.ReadTimeout = 1500; // Wait 1.5s max for data
                testPort.WriteTimeout = 500;
                testPort.Open();

                // Wait for the ESP32 to send its signature data ("Mpu_Values")
                // Your ESP32 sends data every 25ms, so 1500ms is plenty.
                string receivedChunk = "";

                // Read a few times to fill buffer
                // We need to loop briefly because the first read might be partial
                for (int i = 0; i < 5; i++)
                {
                    try { receivedChunk += testPort.ReadExisting(); } catch { }
                    Thread.Sleep(100);
                }

                // Check for your specific protocol signature
                if (receivedChunk.Contains("Mpu_Values") || receivedChunk.Contains(">>"))
                {
                    Debug.Log($"[BT] HANDSHAKE SUCCESS on {port}!");

                    // We found it! Promote this testPort to our main serialPort
                    serialPort = testPort;
                    connectedPort = port;
                    IsConnected = true;
                    keepReading = true;
                    statusMessage = $"Connected: {port}";

                    // Start the permanent read loop
                    readThread = new Thread(ReadDataThread);
                    readThread.Start();

                    isScanning = false;
                    return; // Exit the scanner
                }
                else
                {
                    Debug.Log($"[BT] {port} open, but no valid data. Closing.");
                    testPort.Close();
                }
            }
            catch (Exception)
            {
                // Port busy or not a serial device
                if (testPort != null && testPort.IsOpen) testPort.Close();
            }
        }

        isScanning = false;
        statusMessage = "Device not found. Retrying in 3s...";
        Debug.LogWarning("[BT] Scan complete. Device not found.");

        // Optional: Retry automatically
        Thread.Sleep(3000);
        StartAutoConnection();
    }

    public void Disconnect()
    {
        isScanning = false;
        keepReading = false;
        IsConnected = false;

        // Kill threads safely
        if (connectionThread != null && connectionThread.IsAlive) connectionThread.Abort();
        if (readThread != null && readThread.IsAlive) readThread.Join(500);

        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
        }

        statusMessage = "Disconnected";
        Debug.Log("[BT] Disconnected");
    }

    // --- Sending Methods ---

    private void SendString(string message)
    {
        if (IsConnected && serialPort != null && serialPort.IsOpen)
        {
            try { serialPort.WriteLine(message); }
            catch (Exception e) { Debug.LogWarning($"[BT] Send Error: {e.Message}"); }
        }
    }

    public void SendOff() => SendString("Off>>");
    public void SendRainbow() => SendString("Rainbow>>");
    public void SendIdle() => SendString("Idle>>");

    public void SendColor(Color color, BoardSide side)
    {
        int r = Mathf.RoundToInt(color.r * 255);
        int g = Mathf.RoundToInt(color.g * 255);
        int b = Mathf.RoundToInt(color.b * 255);
        int s = (int)side;
        SendString($"R: {r}, G: {g}, B: {b}, Side: {s}>>");
    }

    // --- Receiving Logic (Main Loop) ---

    private void ReadDataThread()
    {
        while (keepReading && serialPort != null && serialPort.IsOpen)
        {
            try
            {
                string incoming = serialPort.ReadExisting();
                if (!string.IsNullOrEmpty(incoming))
                {
                    ProcessIncomingData(incoming);
                }
            }
            catch (TimeoutException) { }
            catch (Exception e)
            {
                Debug.LogWarning($"[BT] Read Error: {e.Message}");
                Disconnect(); // Auto-disconnect on fatal error
            }
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
        string numStr = source.Substring(startIndex, endIndex - startIndex).Trim();
        if (float.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float result)) return result;
        return 0f;
    }

    private void OnDestroy() => Disconnect();
    private void OnApplicationQuit() => Disconnect();
}