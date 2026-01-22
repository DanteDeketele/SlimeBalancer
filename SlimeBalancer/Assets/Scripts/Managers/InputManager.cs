using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System.Collections;

public class InputManager : BaseManager
{
    [SerializeField] private GameObject DisconnectedUI;

    private string actionMapName = "Player";
    private float prevTimeScale = 1f;
    private bool prevConnectionState = false;

    private InputAction moveAction;
    private Vector2 inputVector;
    private Quaternion inputRotation;
    private Vector3 inputEulerRotation;
    public Vector2 InputVector => inputVector;
    public Quaternion InputRotation => inputRotation;
    public Vector3 InputEulerRotation => inputEulerRotation;

    private BluetoothClient bluetoothClient;

    public UnityEvent OnUp;
    public UnityEvent OnDown;
    public UnityEvent OnLeft;
    public UnityEvent OnRight;
    public UnityEvent<Vector2> OnAnyDirection;
    private Vector2 lastInputVector;

    public bool IsIdle;
    private float lastInputTime;
    public float IdleTime = 10f;
    public UnityEvent OnIdle;
    public UnityEvent OnActive;

    public bool MenuLighting = false;
    private bool isBlink = false;

    private bool isPressedIn = false;

    public bool IsConnected => bluetoothClient != null && bluetoothClient.IsConnected;
    public int BatteryLevel => bluetoothClient.BatteryLevel;

    private void Awake()
    {
        // 3. Find the specific map, then the specific action
        InputActionMap map = InputSystem.actions.FindActionMap(actionMapName);
        if (map == null)
        {
            Debug.LogError($"[InputManager] Could not find Action Map: {actionMapName}");
        }
        else
        {
            moveAction = map.FindAction("Move");
            if (moveAction == null)
            {
                Debug.LogError("[InputManager] Could not find 'Move' action.");
            }
            else
            {
                // Ensure the action is enabled so ReadValue works
                moveAction.Enable();
            }
        }

        // Initialize Bluetooth client
        bluetoothClient = gameObject.AddComponent<BluetoothClient>();

        OnAnyDirection.AddListener(OnAnyDirectionActivated);
    }

    private void OnEnable()
    {
        // Make sure the input action stays enabled when this component is active
        moveAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
    }

    private Vector2 GetInput()
    {
        return moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
    }

    private void OnAnyDirectionActivated(Vector2 direction)
    {
        isPressedIn = true;
    }

    private void Update()
    {
        // Fallback to standard input when Bluetooth is not connected
        if (bluetoothClient != null && bluetoothClient.IsConnected)
        {
            float pitch = -bluetoothClient.pitch;
            float roll = -bluetoothClient.roll;

            float pitchMin = -18f;
            float pitchMax = 18f;
            float rollMin = -18f;
            float rollMax = 18f;
            float xInput = Mathf.Clamp((roll - rollMin) / (rollMax - rollMin) * 2f - 1f, -1f, 1f);
            float yInput = Mathf.Clamp((pitch - pitchMin) / (pitchMax - pitchMin) * 2f - 1f, -1f, 1f);
            inputVector = new Vector2(-xInput, yInput);

            inputRotation = Quaternion.Euler(pitch, 0f, roll);

            inputEulerRotation = new Vector3(pitch, 0f, roll);
        }
        else
        {

            inputVector = GetInput();
            inputRotation = Quaternion.Euler(inputVector.y * 18f, 0f, inputVector.x * 18f);
            inputEulerRotation = new Vector3(inputVector.y * 18f, 0f, inputVector.x * 18f);
        }

        // Detect directional changes and invoke events
        if (inputVector != lastInputVector)
        {
            if (!isPressedIn)
            {
                float sensitivity = 0.85f;
                if (inputVector.y > sensitivity && lastInputVector.y <= sensitivity)
                {
                    OnUp?.Invoke();
                    OnAnyDirection?.Invoke(Vector2.up);
                    Debug.Log("Up input detected");
                }
                else if (inputVector.y < -sensitivity && lastInputVector.y >= -sensitivity)
                {
                    OnDown?.Invoke();
                    OnAnyDirection?.Invoke(Vector2.down);
                    Debug.Log("Down input detected");
                }
                if (inputVector.x > sensitivity && lastInputVector.x <= sensitivity)
                {
                    OnRight?.Invoke();
                    OnAnyDirection?.Invoke(Vector2.right);
                    Debug.Log("Right input detected");
                }
                else if (inputVector.x < -sensitivity && lastInputVector.x >= -sensitivity)
                {
                    OnLeft?.Invoke();
                    OnAnyDirection?.Invoke(Vector2.left);
                    Debug.Log("Left input detected");
                }
            }
            else if (inputVector.magnitude < 0.5f)
            {
                // Reset pressed state when input returns to neutral
                isPressedIn = false;
            }

            if (Vector2.Distance(inputVector, lastInputVector) > 0.05f)
            {
                lastInputTime = Time.time;
            }

            lastInputVector = inputVector;




            if (MenuLighting && !isBlink)
            {
                BluetoothClient.BoardSide side;

                if (inputVector.x > 0.5f)
                {
                    side = BluetoothClient.BoardSide.Right;
                }
                else if (inputVector.x < -0.5f)
                {
                    side = BluetoothClient.BoardSide.Left;
                }
                else if (inputVector.y > 0.5f)
                {
                    side = BluetoothClient.BoardSide.Top;
                }
                else if (inputVector.y < -0.5f)
                {
                    side = BluetoothClient.BoardSide.Bottom;
                }
                else
                {
                    return;
                }

                float brightness = Mathf.Clamp01(Mathf.Max(Mathf.Abs(inputVector.x), Mathf.Abs(inputVector.y)));
                Color color = Color.HSVToRGB(0.33f * brightness, 1f, brightness); // Greenish color based on brightness
                SetLightingEffect(LightingEffect.Custom, color, side);
            }

            if (prevConnectionState != IsConnected && !Application.isEditor)
            {
                
                if (IsConnected)
                {
                    DisconnectedUI.SetActive(false);
                    Time.timeScale = prevTimeScale;
                }
                else
                {
                    DisconnectedUI.SetActive(true);
                    prevTimeScale = Time.timeScale;
                    Time.timeScale = 0f;
                }

                prevConnectionState = IsConnected;
            }

        }

        // Check for idle state
        if (Time.time - lastInputTime >= IdleTime && !IsIdle)
        {
            IsIdle = true;
            bluetoothClient.SendIdle();
            Debug.Log("InputManager: Idle state activated.");
            OnIdle?.Invoke();
            GameManager.SceneManager.LoadScene(GameManager.SceneManager.WaitingSceneName);
        }

        if (Time.time - lastInputTime < IdleTime && IsIdle)
        {
            IsIdle = false;
            Debug.Log("InputManager: Active state restored.");
            bluetoothClient.SendRainbow();
            OnActive?.Invoke();
        }
    }

    private void OnDestroy()
    {
        if (bluetoothClient != null)
        {
            bluetoothClient.Disconnect();
        }
    }

    public enum LightingEffect
    {
        Off,
        Rainbow,
        Idle,
        Custom
    }

    public void SetLightingEffect(LightingEffect effect, Color customColor = default, BluetoothClient.BoardSide side = 0)
    {
        if (bluetoothClient != null && bluetoothClient.IsConnected)
        {
            switch (effect)
            {
                case LightingEffect.Off:
                    bluetoothClient.SendOff();
                    break;
                case LightingEffect.Rainbow:
                    bluetoothClient.SendRainbow();
                    break;
                case LightingEffect.Idle:
                    bluetoothClient.SendIdle();
                    break;
                case LightingEffect.Custom:
                    bluetoothClient.SendColor(customColor, side);
                    break;
            }
        }
    }

    public IEnumerator LedBlink(Color color, int count, float delayBetweenBlinks = 0.5f, BluetoothClient.BoardSide side = 0, Color endColor = default, LightingEffect endEffect = LightingEffect.Custom)
    {
        if (endColor == default)
        {
            endColor = new Color(48f / 255f, 213f / 255f, 150f / 255f);
        }
        yield return bluetoothClient.Blink(color, count, delayBetweenBlinks, side, endColor, endEffect);
    }
}