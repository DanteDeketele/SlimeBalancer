using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InputManager : BaseManager
{
    private string actionMapName = "Player";

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
    public float IdleTime = 60f;
    public UnityEvent OnIdle;
    public UnityEvent OnActive;

    public bool MenuLighting = false;
    private bool isBlink = false;

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
            Debug.Log($"Bluetooth Input - Pitch: {pitch}, Roll: {roll}, Mapped Input: {inputVector}");

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
            if (inputVector.y > 0.9f && lastInputVector.y <= 0.9f)
            {
                OnUp?.Invoke();
                OnAnyDirection?.Invoke(Vector2.up);
                Debug.Log("Up input detected");
            }
            else if (inputVector.y < -0.9f && lastInputVector.y >= -0.9f)
            {
                OnDown?.Invoke();
                OnAnyDirection?.Invoke(Vector2.down);
                Debug.Log("Down input detected");
            }
            if (inputVector.x > 0.9f && lastInputVector.x <= 0.9f)
            {
                OnRight?.Invoke();
                OnAnyDirection?.Invoke(Vector2.right);
                Debug.Log("Right input detected");
            }
            else if (inputVector.x < -0.9f && lastInputVector.x >= -0.9f)
            {
                OnLeft?.Invoke();
                OnAnyDirection?.Invoke(Vector2.left);
                Debug.Log("Left input detected");
            }

            // Update idle status
            lastInputTime = Time.time;

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
        }

        // Check for idle state
        if (Time.time - lastInputTime >= IdleTime)
        {
            IsIdle = true;
            OnIdle?.Invoke();
        }
        else
        {
            IsIdle = false;
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
}