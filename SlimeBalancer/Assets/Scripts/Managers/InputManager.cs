using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InputManager : BaseManager
{
    public Vector2 Input{
        get
        {
            return GetInput();
        }
    }

    private Vector2 GetInput()
    {
        Vector2 input = Vector2.zero;
        if (Keyboard.current.wKey.isPressed)
        {
            input.y += 1;
        }
        if (Keyboard.current.sKey.isPressed)
        {
            input.y -= 1;
        }
        if (Keyboard.current.aKey.isPressed)
        {
            input.x -= 1;
        }
        if (Keyboard.current.dKey.isPressed)
        {
            input.x += 1;
        }
        return input.normalized;
    }
}
