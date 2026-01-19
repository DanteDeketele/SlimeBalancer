using UnityEngine;

public class ScoreManager : BaseManager
{
    private int _score = 0;
    public int Score
    {
        get { return _score; }
        set
        {
            _score = value;
            if (_score > HighScore)
            {
                HighScore = _score;
            }
        }
    }
    public int HighScore
    {
        get { 
            return PlayerPrefs.GetInt("HighScore_"+GameManager.Instance.CurrentGameData.SceneName, 0); 
        }
        set { 
            PlayerPrefs.SetInt("HighScore_"+GameManager.Instance.CurrentGameData.SceneName, value);
        }
    }
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
