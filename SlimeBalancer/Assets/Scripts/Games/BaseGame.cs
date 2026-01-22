
using UnityEngine;

public abstract class BaseGame : MonoBehaviour
{

    public string GameName = "Base Game";
    public bool IsGameActive = false;

    public virtual void Awake()
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
        StartCoroutine(GameManager.InputManager.LedBlink(Color.red, 3, .15f, BluetoothClient.BoardSide.All));
        IsGameActive = false;
        GameManager.ScoreManager.HidePoints();
        Debug.Log($"{GameName} has ended.");
        GameManager.SceneManager.LoadSceneOnTop(GameManager.SceneManager.EndScreen);
        Time.timeScale = 0.5f;
        GameManager.SoundManager.PlaySound(GameManager.SoundManager.GameOverSound, false, false, true);   
    }

}