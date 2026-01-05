using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

public class LoadingscreenUI : MonoBehaviour
{
    private VisualElement _loadingScreen;
    private TextElement _loadingText;
    private VisualElement _progressBar;
    private VisualElement _progressFill;

    public void Awake()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _loadingScreen = root.Q<VisualElement>("LoadingScreen");
        _loadingText = root.Q<TextElement>("LoadingText");
        _progressBar = root.Q<VisualElement>("ProgressBar");
        _progressFill = root.Q<VisualElement>("ProgressFill");
        Assert.IsNotNull(_loadingScreen, "LoadingScreen element not found in UI Document.");
        Assert.IsNotNull(_loadingText, "LoadingText element not found in UI Document.");
        Assert.IsNotNull(_progressBar, "ProgressBar element not found in UI Document.");
        Assert.IsNotNull(_progressFill, "ProgressFill element not found in UI Document.");
    }

    public void SetMessage(string message)
    {
        _loadingText.text = message;
    }

    public void SetProgress(float progress)
    {
        _progressFill.style.width = Length.Percent(Mathf.Clamp01(progress) * 100f);
    }

}
