using UnityEngine;
using UnityEngine.UIElements;
public class EndSceneUI : MonoBehaviour
{
    private VisualElement root;
    private Button restartButton;
    private Button mainMenuButton;
    private Label scoreLabel;
    private Label highScoreLabel;

    private bool restartSelected = false;

    private Vector2 lastInput = Vector2.zero;

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;
        restartButton = root.Q<Button>("RestartButton");
        mainMenuButton = root.Q<Button>("MainMenuButton");
        scoreLabel = root.Q<Label>("ScoreLabel");
        highScoreLabel = root.Q<Label>("HighScoreLabel");
        restartButton.clicked += OnRestartButtonClicked;
        mainMenuButton.clicked += OnMainMenuButtonClicked;

        // Set score and high score labels
        int finalScore = GameManager.ScoreManager.Score;
        scoreLabel.text = $"Score: {finalScore}";
        int highScore = GameManager.ScoreManager.HighScore;
        highScoreLabel.text = $"High Score: {highScore}";

        // Initially focus on the restart button
        restartButton.Focus();

        GameManager.InputManager.OnLeft.AddListener(() =>
        {
            restartButton.Focus();
            restartSelected = true;
        });
        GameManager.InputManager.OnRight.AddListener(() =>
        {
            mainMenuButton.Focus();
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
        restartButton.clicked -= OnRestartButtonClicked;
        mainMenuButton.clicked -= OnMainMenuButtonClicked;
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