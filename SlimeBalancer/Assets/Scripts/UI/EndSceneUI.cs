using UnityEngine;
using UnityEngine.UIElements;
public class EndSceneUI : MonoBehaviour
{
    private VisualElement root;
    private VisualElement restartButton;
    private VisualElement mainMenuButton;
    private Label scoreLabel;
    private Label highScoreLabel;

    private bool restartSelected = false;

    private Vector2 lastInput = Vector2.zero;

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;
        restartButton = root.Q<VisualElement>("RestartButton");
        mainMenuButton = root.Q<VisualElement>("MainMenuButton");
        restartButton.AddToClassList("focused");
        scoreLabel = root.Q<Label>("ScoreLabel");
        highScoreLabel = root.Q<Label>("HighScoreLabel");

        // Set score and high score labels
        int finalScore = GameManager.ScoreManager.Score;
        scoreLabel.text = $"Score: {finalScore}";
        int highScore = GameManager.ScoreManager.HighScore;
        highScoreLabel.text = $"High Score: {highScore}";

        // Initially focus on the restart button
        restartButton.Focus();

        GameManager.InputManager.OnLeft.AddListener(() =>
        {
            restartButton.AddToClassList("focused");
            mainMenuButton.RemoveFromClassList("focused");
            restartSelected = true;
        });
        GameManager.InputManager.OnRight.AddListener(() =>
        {
            mainMenuButton.AddToClassList("focused");
            restartButton.RemoveFromClassList("focused");
            restartSelected = false;
        });
        GameManager.InputManager.OnDown.AddListener(() =>
        {
            if (restartSelected)
            {
                OnRestartButtonClicked();
            }
            else
            {
                OnMainMenuButtonClicked();
            }
        });

    }


    private void OnDisable()
    {
        GameManager.InputManager.OnLeft.RemoveAllListeners();
        GameManager.InputManager.OnRight.RemoveAllListeners();
        GameManager.InputManager.OnDown.RemoveAllListeners();
    }

    private void OnRestartButtonClicked()
    {
        GameManager.Instance.LoadGame(GameManager.Instance.CurrentGameData.SceneName);
    }

    private void OnMainMenuButtonClicked()
    {
        GameManager.SceneManager.LoadScene(GameManager.SceneManager.MainMenuSceneName);
    }
}