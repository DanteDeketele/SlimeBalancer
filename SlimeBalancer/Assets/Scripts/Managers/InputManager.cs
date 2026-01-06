using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : BaseManager
{
    private string actionMapName = "Player";

    private InputAction moveAction;
    private Vector2 inputVector;
    public Vector2 InputVector => inputVector;

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
    }

    private Vector2 GetInput()
    {
        return moveAction.ReadValue<Vector2>();
    }

    private void Update()
    {
        inputVector = GetInput();
    }
}