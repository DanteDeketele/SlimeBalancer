using UnityEngine;

public class ScoreManager : BaseManager
{
    public int Score = 0;
    public GameObject scoreUIPrefab;
    private ScoreUI scoreUIInstance;

    private void Start()
    {
        LogInitialization();
        if (scoreUIPrefab != null)
        {
            GameObject uiObject = Instantiate(scoreUIPrefab);
            scoreUIInstance = uiObject.GetComponent<ScoreUI>();
            scoreUIInstance.gameObject.SetActive(false);
        }
    }
    public void AddScore(int points)
    {
        Debug.Log($"Added {points} points to the score.");
        Score += points;
        scoreUIInstance.SetScore(Score.ToString());
    }
    public void ResetScore()
    {
        Debug.Log("Score reset to 0.");
        Score = 0;
        scoreUIInstance.SetScore(Score.ToString());
    }

    public void RemoveScore(int points)
    {
        Debug.Log($"Removed {points} points from the score.");
        Score -= points;
        scoreUIInstance.SetScore(Score.ToString());
    }

    public void ShowPoints()
    {
        if (scoreUIInstance != null)
        {
            scoreUIInstance.gameObject.SetActive(true);
        }
    }

    public void HidePoints()
    {
        if (scoreUIInstance != null)
        {
            scoreUIInstance.gameObject.SetActive(false);
        }
    }
}
