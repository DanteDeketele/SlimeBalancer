
using UnityEngine;

public abstract class BaseManager : MonoBehaviour
{
    public void LogInitialization()
    {
        Debug.Log($"{GetType().Name} initialized.");
    }

    public virtual void ResetManager()
    {
        Debug.Log($"{GetType().Name} reset to default state.");
    }


}