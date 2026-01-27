using UnityEngine;
using UnityEngine.UIElements;
public class QuitUI : MonoBehaviour
{
    private VisualElement root;
    private VisualElement returnButton;
    private VisualElement quitButton;

    private bool restartSelected = true;

    private Vector2 lastInput = Vector2.zero;
    private bool inputInUse = false;
    private float inputDelay = 0f; // seconds
    private float timer = 0f;

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;
        returnButton = root.Q<VisualElement>("Return");
        quitButton = root.Q<VisualElement>("Quit");
        returnButton.AddToClassList("focused");
        returnButton.AddToClassList("disabled");
        quitButton.AddToClassList("disabled");

        // Initially focus on the restart button
        returnButton.Focus();

        GameManager.InputManager.OnLeft.AddListener(() =>
        {
            if (!inputInUse) return;
            if (restartSelected) return;
            returnButton.AddToClassList("focused");
            quitButton.RemoveFromClassList("focused");
            GameManager.SoundManager.PlaySound(GameManager.SoundManager.UISelectSound);
            restartSelected = true;
        });
        GameManager.InputManager.OnRight.AddListener(() =>
        {
            if (!inputInUse) return;
            if (!restartSelected) return;
            quitButton.AddToClassList("focused");
            returnButton.RemoveFromClassList("focused");
            GameManager.SoundManager.PlaySound(GameManager.SoundManager.UISelectSound);
            restartSelected = false;
        });
        GameManager.InputManager.OnDown.AddListener(() =>
        {
            if (!inputInUse) return;
            if (restartSelected)
            {
                GameManager.SceneManager.LoadScene(GameManager.SceneManager.SettingsSceneName);
                GameManager.SoundManager.PlaySound(GameManager.SoundManager.GameSelectSound);

            }
            else
            {
                Application.Quit();

            }
        });

    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= inputDelay)
        {
            inputInUse = true;
            quitButton.RemoveFromClassList("disabled");
            returnButton.RemoveFromClassList("disabled");
        }
    }

    private void OnDisable()
    {
        GameManager.InputManager.OnLeft.RemoveAllListeners();
        GameManager.InputManager.OnRight.RemoveAllListeners();
        GameManager.InputManager.OnDown.RemoveAllListeners();
    }
}