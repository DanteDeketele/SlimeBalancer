
using UnityEngine;

public abstract class BaseGame : MonoBehaviour
{
    public string GameName = "Base Game";
    public bool IsGameActive = false;

    public virtual void StartGame()
    {
        IsGameActive = true;
        Debug.Log($"{GameName} has started.");
    }

    public virtual void UpdateGame()
    {
    }

    public virtual void EndGame()
    {
        IsGameActive = false;
        Debug.Log($"{GameName} has ended.");
    }

}