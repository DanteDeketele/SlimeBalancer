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
    private bool inputInUse = false;
    private float inputDelay = 3f; // seconds
    private float timer = 0f;

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;
        restartButton = root.Q<VisualElement>("RestartButton");
        mainMenuButton = root.Q<VisualElement>("MainMenuButton");
        restartButton.AddToClassList("focused");
        restartButton.AddToClassList("disabled");
        mainMenuButton.AddToClassList("disabled");
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
            if (!inputInUse) return;
            restartButton.AddToClassList("focused");
            mainMenuButton.RemoveFromClassList("focused");
            restartSelected = true;
        });
        GameManager.InputManager.OnRight.AddListener(() =>
        {
            if (!inputInUse) return;
            mainMenuButton.AddToClassList("focused");
            restartButton.RemoveFromClassList("focused");
            restartSelected = false;
        });
        GameManager.InputManager.OnDown.AddListener(() =>
        {
            if (!inputInUse) return;
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

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= inputDelay)
        {
            inputInUse = true;
            mainMenuButton.RemoveFromClassList("disabled");
            restartButton.RemoveFromClassList("disabled");
        }
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