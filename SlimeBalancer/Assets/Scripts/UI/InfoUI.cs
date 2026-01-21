using UnityEngine;
using UnityEngine.UIElements;

public class InfoUI : MonoBehaviour
{
    private VisualElement root;
    private Label infoLabel;
    private void OnEnable()
    {
        GameManager.InputManager.OnDown.AddListener(Next);
        GameManager.InputManager.OnUp.AddListener(Back);

        var uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        root.Q<Label>("game-title").text = "Game: " + GameManager.Instance.CurrentGameData.GameName;
        Label explainLabel = root.Q<Label>("info1");
        foreach (string line in GameManager.Instance.CurrentGameData.Explination)
        {
            // a new label with the same style as explainLabel
            Label lineLabel = new Label(line);
            lineLabel.name = "info-line";
            lineLabel.AddToClassList("info-text");
            explainLabel.parent.Add(lineLabel);
        }
        //remove originals
        explainLabel.RemoveFromHierarchy();
    }

    // unload event listeners
    private void OnDisable()
    {
        GameManager.InputManager.OnDown.RemoveListener(Next);
        GameManager.InputManager.OnUp.RemoveListener(Back);
    }

    public void Next()
    {
        GameManager.Instance.LoadGame(GameManager.Instance.CurrentGameData.SceneName);
        GameManager.SoundManager.PlaySound(GameManager.SoundManager.UISelectSound);
    }

    public void Back()
    {
        GameManager.SceneManager.LoadScene(GameManager.SceneManager.MainMenuSceneName);
        GameManager.SoundManager.PlaySound(GameManager.SoundManager.UISelectSound);
    }

    public void SetInfoText(string text)
    {
        infoLabel.text = text;
    }
}