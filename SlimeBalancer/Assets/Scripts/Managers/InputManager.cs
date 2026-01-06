using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InputManager : BaseManager
{
    public InputAction MoveInput;

    private void Awake()
    {
        MoveInput = InputSystem.actions.FindAction("Move");
    }

    public Vector2 Input{
        get
        {
            return GetInput();
        }
    }

    private Vector2 GetInput()
    {
        if (MoveInput != null)
        {
            return MoveInput.ReadValue<Vector2>();
        }
        return Vector2.zero;
    }
}
