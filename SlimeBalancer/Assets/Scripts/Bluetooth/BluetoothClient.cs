using System;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using UnityEngine;

// Simple Bluetooth SPP client for Windows using a COM port.
// Pair your ESP32 ("SlimeBalancer") in Windows first. Windows will create a COM port for SPP.
// Set that COM port in `portName` (e.g., "COM5").
public class BluetoothClient : MonoBehaviour
{
    [Header("Serial Port Settings")]
    [Tooltip("Windows COM port assigned to the ESP32 SPP (e.g., COM5). Pair the device first in Windows.")]
    public string portName = "COM5";

    [Tooltip("Baud rate must match Serial.begin on ESP32.")]
    public int baudRate = 115200;

    [Tooltip("Connect automatically on Start().")]
    public bool connectOnStart = true;

    [Header("Reconnect Settings")]
    [Tooltip("Try to reconnect if the connection is lost.")]
    public bool autoReconnect = true;

    [Tooltip("Seconds between reconnect attempts.")]
    public float reconnectIntervalSeconds = 2f;

    [Header("Latest Sensor Values (read-only)")]
    public float pitch;
    public float roll;
    public float temperature;

    public bool IsConnected => _port != null && _port.IsOpen;

    private SerialPort _port;
    private Thread _readThread;
    private volatile bool _running;
    private readonly object _lock = new object();

    private volatile bool _connecting;
    private volatile bool _requestReconnect;
    private float _nextReconnectTime;

    void OnEnable()
    {
        // Initialize next reconnect time to avoid immediate tight-loop attempts
        _nextReconnectTime = Time.realtimeSinceStartup + reconnectIntervalSeconds;

        if (connectOnStart)
        {
            // Kick off connection without blocking main thread
            QueueConnectAttempt();
        }
    }

    void Update()
    {
        if (!autoReconnect) return;

        // When a background thread asked for a reconnect, schedule next attempt on main thread
        if (_requestReconnect)
        {
            _requestReconnect = false;
            _nextReconnectTime = Time.realtimeSinceStartup + reconnectIntervalSeconds;
        }

        // If not connected and not currently trying, attempt on interval
        if (!IsConnected && !_connecting && Time.realtimeSinceStartup >= _nextReconnectTime)
        {
            _nextReconnectTime = Time.realtimeSinceStartup + reconnectIntervalSeconds;
            QueueConnectAttempt();
        }
    }

    void OnDestroy()
    {
        Disconnect();
    }

    void OnApplicationQuit()
    {
        Disconnect();
    }

    // Schedule a background connection attempt
    private void QueueConnectAttempt()
    {
        if (_connecting || IsConnected) return;
        _connecting = true;
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                TryConnect();
            }
            finally
            {
                _connecting = false;
                if (!IsConnected && autoReconnect)
                {
                    // Ask main thread to schedule the next reconnect attempt
                    _requestReconnect = true;
                }
            }
        });
    }

    private static bool PortExists(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        try
        {
            var ports = SerialPort.GetPortNames();
            return ports != null && ports.Contains(name, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    // This is now only called on a background worker by QueueConnectAttempt
    public void TryConnect()
    {
        Debug.Log("BluetoothClient: Attempting to connect...");
        if (IsConnected)
        {
            return;
        }

        try
        {
            // If a specific port is set, try it first; otherwise scan available ports
            if (!string.IsNullOrEmpty(portName))
            {
                if (!PortExists(portName))
                {
                    // Port not present on this machine; skip opening and let keyboard fallback be used
                    Debug.LogWarning($"BluetoothClient: Configured port '{portName}' not found. Using fallback input. Will retry later.");
                    return;
                }
                OpenPort(portName);
            }
            else
            {
                string[] ports = SerialPort.GetPortNames();
                if (ports == null || ports.Length == 0)
                {
                    Debug.LogWarning("BluetoothClient: No COM ports found. Ensure the device is paired and a COM port exists.");
                }
                else
                {
                    foreach (var p in ports)
                    {
                        try
                        {
                            OpenPort(p);
                            if (IsConnected)
                            {
                                portName = p; // adopt discovered port
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"BluetoothClient: Could not open {p}. {ex.GetType().Name}: {ex.Message}");
                            SafeDisposePort();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"BluetoothClient: Failed to connect. {ex.GetType().Name}: {ex.Message}");
            SafeDisposePort();
        }
    }

    private void OpenPort(string name)
    {
        _port = new SerialPort(name, baudRate)
        {
            ReadTimeout = 500,
            WriteTimeout = 500,
            NewLine = ">>", // Messages end with ">>\r\n" on the ESP32
            DtrEnable = false,
            RtsEnable = false
        };
        _port.Open();
        _running = true;

        _readThread = new Thread(ReadLoop)
        {
            IsBackground = true,
            Name = "BluetoothClientReadLoop"
        };
        _readThread.Start();

        Debug.Log($"BluetoothClient: Opened {name} @ {baudRate}.");
    }

    public void Disconnect()
    {
        _running = false;
        try
        {
            if (_readThread != null && _readThread.IsAlive)
            {
                // Interrupt blocking reads by closing the port first
                try { _port?.Close(); } catch { /* ignore */ }
                if (!_readThread.Join(500))
                {
                    try { _readThread.Abort(); } catch { /* ignore */ }
                }
            }
        }
        catch { /* ignore */ }
        finally
        {
            SafeDisposePort();
            _readThread = null;
        }
    }

    private void SafeDisposePort()
    {
        try
        {
            if (_port != null)
            {
                if (_port.IsOpen)
                {
                    _port.Close();
                }
                _port.Dispose();
            }
        }
        catch { /* ignore */ }
        finally
        {
            _port = null;
        }
    }

    private void ReadLoop()
    {
        var invariant = CultureInfo.InvariantCulture;

        while (_running && _port != null)
        {
            try
            {
                // Read until ">>" (per NewLine)
                string payload = _port.ReadLine();
                if (string.IsNullOrEmpty(payload))
                    continue;

                // Expecting: "Mpu_Values: P:<p>,R:<r>,T:<t>"
                if (!payload.Contains("Mpu_Values:"))
                    continue;

                // Quick parse without allocations; split is fine for simplicity
                // Example: Mpu_Values: P:12.3,R:-4.56,T:37.2
                float p, r, t;
                if (TryParseValues(payload, invariant, out p, out r, out t))
                {
                    lock (_lock)
                    {
                        pitch = p;
                        roll = r;
                        temperature = t;
                    }
                }
            }
            catch (TimeoutException)
            {
                // benign
            }
            catch (InvalidOperationException)
            {
                // Port closed during read
                break;
            }
            catch (ThreadAbortException)
            {
                break;
            }
            catch (System.IO.IOException ex)
            {
                // Device removed or IO error; close and break so reconnect can occur
                Debug.LogWarning($"BluetoothClient: Read error: IOException: {ex.Message}");
                try { _port?.Close(); } catch { }
                break;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"BluetoothClient: Read error: {ex.GetType().Name}: {ex.Message}");
                // small backoff to avoid busy loop on repeated failures
                Thread.Sleep(50);
            }
        }

        // Ensure port closed on loop exit
        try { _port?.Close(); } catch { }

        // Signal reconnect if enabled (on main thread via Update)
        if (autoReconnect)
        {
            _requestReconnect = true;
        }
    }

    private static bool TryParseValues(string payload, IFormatProvider fmt, out float p, out float r, out float t)
    {
        p = r = t = 0f;

        // Remove prefix if present
        int idx = payload.IndexOf('P');
        if (idx < 0)
        {
            // Try to find first digit sign
            idx = payload.IndexOf(':');
            if (idx >= 0 && idx + 1 < payload.Length) idx++;
            else idx = 0;
        }

        // Split by comma for simplicity
        // Expect tokens like "P:12.3" "R:-4.56" "T:37.2"
        try
        {
            string work = payload.Trim();
            // Trim optional leading label
            int label = work.IndexOf(':');
            if (label >= 0 && label + 1 < work.Length)
            {
                work = work.Substring(label + 1).Trim();
            }

            var parts = work.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) return false;

            // P
            int pidx = parts[0].IndexOf(':');
            string ps = pidx >= 0 ? parts[0].Substring(pidx + 1) : parts[0];
            // R
            int ridx = parts[1].IndexOf(':');
            string rs = ridx >= 0 ? parts[1].Substring(ridx + 1) : parts[1];
            // T
            int tidx = parts[2].IndexOf(':');
            string ts = tidx >= 0 ? parts[2].Substring(tidx + 1) : parts[2];

            p = (float)double.Parse(ps, fmt);
            r = (float)double.Parse(rs, fmt);
            t = (float)double.Parse(ts, fmt);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public enum BoardSide
    {
        Left = 1,
        Right = 3,
        Top = 4,
        Bottom = 2,
        All = 0
    }

    public void SendColor(Color color, BoardSide side)
    {
        if (!IsConnected) return;
        try
        {
            // Format: "“R: 255, G: 127, B: 0, Side: 1>>”

            int r = Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255);
            int g = Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255);
            int b = Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255);
            string cmd = $"R: {r},G: {g},B: {b},Side: {(int)side}";
            _port.WriteLine(cmd);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"BluetoothClient: SendColor error: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public void SendRainbow()
    {
        if (!IsConnected) return;
        try
        {
            // Format: “Rainbow>>”
            string cmd = "Rainbow";
            _port.WriteLine(cmd);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"BluetoothClient: SendRainbow error: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public void SendOff()
    {
        if (!IsConnected) return;
        try
        {
            // Format: “Off>>”
            string cmd = "Off";
            _port.WriteLine(cmd);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"BluetoothClient: off error: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public void SendIdle()
    {
        if (!IsConnected) return;
        try
        {
            // Format: “Idle>>”
            string cmd = "Idle";
            _port.WriteLine(cmd);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"BluetoothClient: idle error: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
