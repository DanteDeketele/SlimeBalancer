
using UnityEngine;

public abstract class BaseGame : MonoBehaviour
{
    public string gameName = "Base Game";

    public virtual void StartGame()
    {
        Debug.Log($"{gameName} has started.");
    }

    public virtual void UpdateGame()
    {
    }

    public virtual void EndGame()
    {
        Debug.Log($"{gameName} has ended.");
    }

}