
using UnityEngine;

public abstract class BaseGame : MonoBehaviour
{

    public string GameName = "Base Game";
    public bool IsGameActive = false;

    void Awake()
    {
        if (GameManager.Instance == null)
        {
            // Go back to main menu if no GameManager exists
            GameManager.GameSceneNameToSkipTo = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }

    public virtual void StartGame()
    {
        IsGameActive = true;
        Debug.Log($"{GameName} has started.");
    }

    public virtual void UpdateGame()
    {
    }

    public virtual void EndGame(bool won = false)
    {
        IsGameActive = false;
        GameManager.ScoreManager.HidePoints();
        Debug.Log($"{GameName} has ended.");
        GameManager.SceneManager.LoadSceneOnTop(GameManager.SceneManager.EndScreen);
        Time.timeScale = 0.5f;
    }

}