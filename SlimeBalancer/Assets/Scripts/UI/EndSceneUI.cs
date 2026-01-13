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

    }

    // Input handling
    public void Update()
    {
        Vector2 navigation = GameManager.InputManager.InputVector;
        Vector2 nav = Vector2.zero;
        if (navigation.y > 0.75f)
            nav.y = 1;
        else if (navigation.y < -0.75f)
            nav.y = -1;
        if (navigation.x > 0.75f)
            nav.x = 1;
        else if (navigation.x < -0.75f)
            nav.x = -1;

        if (nav != lastInput)
        {
            if (nav.x == 1)
            {
                if (restartSelected)
                {
                    restartButton.Blur();
                    mainMenuButton.Focus();
                    restartSelected = false;
                }
            }
            else if (nav.x == -1)
            {
                if (!restartSelected)
                {
                    mainMenuButton.Blur();
                    restartButton.Focus();
                    restartSelected = true;
                }
            }
            else if (nav.y == -1)
            {
                // use selected button
                if (restartSelected)
                    OnRestartButtonClicked();
                else
                    OnMainMenuButtonClicked();
            }
            lastInput = nav;
        }

    }

    private void OnDisable()
    {
        restartButton.clicked -= OnRestartButtonClicked;
        mainMenuButton.clicked -= OnMainMenuButtonClicked;
    }

    private void OnRestartButtonClicked()
    {
        // Logic to restart the game
        GameManager.Instance.LoadGame(GameManager.Instance.CurrentGameData.SceneName);
    }

    private void OnMainMenuButtonClicked()
    {
        // Logic to return to main menu
        GameManager.SceneManager.LoadScene(GameManager.SceneManager.MainMenuSceneName);
    }
}