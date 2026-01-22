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

    private int[] batteryRecords = new int[200];
    private int lastBatteryLevel = -1;
    public int BatteryLevel
    {
        get
        {
            int sum = 0;
            int count = 0;
            foreach (var level in batteryRecords)
            {
                if (level > 0)
                {
                    sum += level;
                    count++;
                }
            }
            int percentage = count > 0 ? sum / count : 0;
            if (Mathf.Abs(percentage - lastBatteryLevel) >= 2)
            {
                lastBatteryLevel = percentage;
                return percentage;
            }
            else
            {
                return lastBatteryLevel;
            }
        }
    }

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
    public float postOpenResetDelaySec = 0.5f;
    [Tooltip("Seconds to listen for handshake text after the initial reset delay.")]
    public float handshakeListenWindowSec = 1.0f;
    [Tooltip("Filters available ports to only those that can be opened briefly. Helps hide stale Bluetooth COM ports.")]
    public bool filterToOpenablePorts = true;
    [Tooltip("Timeout in ms used by the quick open-check when filtering ports.")]
    public int openCheckTimeoutMs = 300;

    [Header("Disconnect Watchdog")]
    [Tooltip("If no data arrives for this many seconds, start probing the port to detect a dead link.")]
    public float inactivityDisconnectSec = 0.5f; // faster detection
    [Tooltip("How often to probe the port while inactive (seconds).")]
    public float probeIntervalSec = 0.75f; // probe sooner
    [Tooltip("When probing, write a newline to trigger I/O errors on dead ports.")]
    public bool enableProbeWrite = true;

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

    // Connection watchdog
    private DateTime lastDataUtc = DateTime.MinValue;
    private DateTime lastProbeUtc = DateTime.MinValue;
    private volatile bool portErrorFlag = false;

    // App lifecycle flag to avoid using Unity APIs off main thread
    private volatile bool isAppStopping = false;

    public enum BoardSide { All = 0, Top = 4, Right = 3, Bottom = 2, Left = 1 }

    private void Awake()
    {
#if UNITY_EDITOR
        // Ensure disconnect when exiting play mode in editor
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
    }

    private void Start()
    {
        if (!Application.isPlaying) return;
        StartAutoConnection();
    }

    private void OnDisable()
    {
        isAppStopping = true;
        Disconnect();
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
        // Removed Application.isPlaying check to allow calls from worker thread safely
        if (isScanning || IsConnected || isAppStopping) return;

        isScanning = true;
        statusMessage = "Scanning all ports...";

        // Run the scanner in a background task so Unity doesn't freeze
        Task.Run(() => ScanAllPortsParallel());
    }

    public void ConnectToPort(string portName)
    {
        if (!Application.isPlaying) return;
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
        if (!Application.isPlaying) return;
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
                    RtsEnable = false,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                    DataBits = 8,
                    Handshake = Handshake.None,
                    NewLine = "\n"
                };
                p.Open();
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
        if (isAppStopping) { isScanning = false; return; }
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
            await Task.WhenAll(portTasks);
        }
        catch (OperationCanceledException)
        {
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
        if (isAppStopping) return;
        SerialPort testPort = null;
        try
        {
            if (token.IsCancellationRequested) return;

            if (logVerbose) Debug.Log($"[BT] Testing {port} @ {baudRate}bps (DTR/RTS={(useDtrRts ? "on" : "off")})");
            testPort = new SerialPort(port, baudRate)
            {
                ReadTimeout = 250, // keep short so reads don’t block
                WriteTimeout = 250,
                DtrEnable = useDtrRts,
                RtsEnable = useDtrRts,
                Parity = Parity.None,
                StopBits = StopBits.One,
                DataBits = 8,
                Handshake = Handshake.None,
                NewLine = "\n"
            };

            try
            {
                testPort.Open();
                try { testPort.DiscardInBuffer(); testPort.DiscardOutBuffer(); } catch { }
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
                        if (logVerbose) Debug.Log($"[BT] {port} RX: {chunk.Replace("\n", "\\n").Replace("\r", "\\r")}");
                        if (accumulated.Contains("Mpu_Values") || accumulated.Contains(">>"))
                        {
                            if (!IsConnected)
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
        lock (this)
        {
            if (isAppStopping)
            {
                try { port.Close(); } catch { }
                return;
            }

            if (IsConnected)
            {
                try { port.Close(); } catch { }
                return;
            }

            IsConnected = true;
            isScanning = false;
            serialPort = port; // Adopt the open port
            connectedPort = portName;
            statusMessage = $"Connected: {portName}";

            // Attach event handlers to detect link loss
            try
            {
                serialPort.ErrorReceived += SerialPort_ErrorReceived;
                serialPort.PinChanged += SerialPort_PinChanged;
            }
            catch { }

            lastDataUtc = DateTime.UtcNow;
            lastProbeUtc = DateTime.UtcNow;
            portErrorFlag = false;

            try { scanTokenSource?.Cancel(); } catch { }

            Debug.Log($"[BT] SUCCESS! Connected to {portName}");

            keepReading = true;
            readThread = new Thread(ReadDataThread) { IsBackground = true };
            readThread.Start();
        }
    }

    private void RetryScan()
    {
        if (isAppStopping) { isScanning = false; return; }
        isScanning = false;
        statusMessage = "Device not found. Retrying...";
        if (logVerbose) Debug.Log("[BT] Scan failed. Retrying in 3s...");
        Thread.Sleep(1000);
        StartAutoConnection();
    }

    public void Disconnect()
    {
        try { scanTokenSource?.Cancel(); } catch { }
        isScanning = false;
        keepReading = false;
        IsConnected = false;
        Debug.Log("[BT] Disconnecting...");

        // Close port first to unblock any pending I/O
        try
        {
            if (serialPort != null)
            {
                try
                {
                    serialPort.ErrorReceived -= SerialPort_ErrorReceived;
                    serialPort.PinChanged -= SerialPort_PinChanged;
                }
                catch { }

                if (serialPort.IsOpen) serialPort.Close();
            }
        }
        catch { }

        // Briefly join read thread
        try { if (readThread != null && readThread.IsAlive) readThread.Join(100); } catch { }

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

    // --- Receiving Logic with watchdog ---
    private void ReadDataThread()
    {
        while (keepReading && serialPort != null && serialPort.IsOpen)
        {
            bool hadData = false;

            try
            {
                string incoming = serialPort.ReadExisting();
                if (!string.IsNullOrEmpty(incoming))
                {
                    hadData = true;
                    lastDataUtc = DateTime.UtcNow;
                    ProcessIncomingData(incoming);
                }
            }
            catch
            {
                // Any I/O error means the port is gone.
                Disconnect();
                StartAutoConnection();
                break;
            }

            if (!hadData)
            {
                // If we haven't seen data for a while, probe to force error on dead RFCOMM links.
                var now = DateTime.UtcNow;

                // Port disappeared from system? Treat as lost.
                try
                {
                    var ports = SerialPort.GetPortNames();
                    if (!string.IsNullOrEmpty(connectedPort) && Array.IndexOf(ports, connectedPort) < 0)
                    {
                        if (logVerbose) Debug.LogWarning($"[BT] Port {connectedPort} no longer present. Disconnecting.");
                        Disconnect();
                        StartAutoConnection();
                        break;
                    }
                }
                catch { }

                bool inactivityExceeded = (now - lastDataUtc).TotalSeconds >= Math.Max(0.5f, inactivityDisconnectSec);
                bool intervalElapsed = (now - lastProbeUtc).TotalSeconds >= Math.Max(0.25f, probeIntervalSec);

                if (portErrorFlag || (inactivityExceeded && intervalElapsed))
                {
                    lastProbeUtc = now;
                    try
                    {
                        if (enableProbeWrite)
                        {
                            // Send a benign newline; will throw if link is dead
                            serialPort.Write("\n");
                        }
                        // Accessing modem status can also trigger exceptions when the link is dead
                        var _ = serialPort.CDHolding;
                    }
                    catch
                    {
                        if (logVerbose) Debug.LogWarning("[BT] Port probe failed. Disconnecting quickly.");
                        Disconnect();
                        StartAutoConnection();
                        break;
                    }
                    finally
                    {
                        portErrorFlag = false; // reset; will be set again by events if errors persist
                    }
                }
            }

            Thread.Sleep(5);
        }
    }

    private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
    {
        portErrorFlag = true;
        if (logVerbose) Debug.LogWarning($"[BT] Serial error: {e.EventType}");
    }

    private void SerialPort_PinChanged(object sender, SerialPinChangedEventArgs e)
    {
        if (logVerbose) Debug.Log($"[BT] Pin changed: {e.EventType}");
        // Some adapters toggle pins when link drops; mark to probe soon.
        portErrorFlag = true;
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
        else if (msg.StartsWith("Battery_Level:"))
        {
            try
            {
                int level = (int)ExtractValue(msg, "Battery_Level: ", "");
                if (level >= 0 && level <= 100)
                {
                    // Shift records and add new level
                    for (int i = batteryRecords.Length - 1; i > 0; i--)
                    {
                        batteryRecords[i] = batteryRecords[i - 1];
                    }
                    batteryRecords[0] = level;
                }
            }
            catch { }
        }
        else
        {
            Debug.Log($"[BT] Unrecognized message: {msg}");
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

    private void OnDestroy()
    {
        isAppStopping = true;
        Disconnect();
    }

    private void OnApplicationQuit()
    {
        isAppStopping = true;
        Disconnect();
    }

#if UNITY_EDITOR
    private void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange change)
    {
        if (change == UnityEditor.PlayModeStateChange.ExitingPlayMode)
        {
            isAppStopping = true;
            Disconnect();
        }
    }
#endif
}