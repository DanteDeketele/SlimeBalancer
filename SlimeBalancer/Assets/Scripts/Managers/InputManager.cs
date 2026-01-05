using UnityEngine;
using UnityEngine.Events;

public class InputManager : BaseManager
{
    public UnityAction<Vector2> OnDirectionreceived;
    private Vector2 _lastDirection;

    public void Update()
    {
        Vector2 direction = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (direction != _lastDirection)
        {
            _lastDirection = direction;
            OnDirectionreceived?.Invoke(direction);
        }
    }

    public void AddInputListner(UnityAction<Vector2> listener)
    {
        OnDirectionreceived += listener;
    }
}
