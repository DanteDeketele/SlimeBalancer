using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : BaseManager
{
    private string actionMapName = "Player";

    private InputAction moveAction;
    private Vector2 inputVector;
    public Vector2 InputVector => inputVector;

    private BluetoothClient bluetoothClient;

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
            float pitch = bluetoothClient.pitch;
            float roll = bluetoothClient.roll;

            float pitchMin = -30f;
            float pitchMax = 30f;
            float rollMin = -30f;
            float rollMax = 30f;
            float xInput = Mathf.Clamp((roll - rollMin) / (rollMax - rollMin) * 2f - 1f, -1f, 1f);
            float yInput = Mathf.Clamp((pitch - pitchMin) / (pitchMax - pitchMin) * 2f - 1f, -1f, 1f);
            inputVector = new Vector2(xInput, yInput);
        }
        else
        {
            inputVector = GetInput();
        }
    }

    private void OnDestroy()
    {
        if (bluetoothClient != null)
        {
            bluetoothClient.Disconnect();
        }
    }
}