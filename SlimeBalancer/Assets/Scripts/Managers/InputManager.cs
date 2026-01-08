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
            return;
        }

        moveAction = map.FindAction("Move");
        if (moveAction == null)
        {
            Debug.LogError("[InputManager] Could not find 'Move' action.");
        }

        // Initialize Bluetooth client
        bluetoothClient = gameObject.AddComponent<BluetoothClient>();
    }

    private Vector2 GetInput()
    {
        return moveAction.ReadValue<Vector2>();
    }

    private void Update()
    {
        // inputVector = GetInput();

        if (bluetoothClient.IsConnected)
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
            Debug.Log($"Input Vector from Bluetooth: {inputVector}");
        }
    }

    private void OnDestroy()
    {
        bluetoothClient.Disconnect();
    }
}