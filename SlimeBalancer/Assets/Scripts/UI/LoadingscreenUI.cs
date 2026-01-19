using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class LoadingscreenUI : MonoBehaviour
{
    private VisualElement _loadingScreen;
    private Label _loadingText; // Changed to Label (more specific than TextElement)
    private VisualElement _progressFill;

    // Optional: Cache opacity for fade out later
    private VisualElement _root;

    public void Awake()
    {
        var doc = GetComponent<UIDocument>();
        _root = doc.rootVisualElement;

        _loadingScreen = _root.Q<VisualElement>("LoadingScreen");
        _loadingText = _root.Q<Label>("LoadingText");

        // Note: We don't necessarily need reference to the Bar Rail, just the Fill
        _progressFill = _root.Q<VisualElement>("ProgressFill");

        // Validate
        if (_loadingText == null || _progressFill == null)
        {
            Debug.LogError("Loading Screen UI: Missing visual elements. Check UXML names.");
        }

        // Initialize
        SetProgress(0f);
        SetMessage("LOADING...");
    }

    public void SetMessage(string message)
    {
        if (_loadingText != null)
            _loadingText.text = message.ToUpper(); // Force Uppercase for style
    }

    public void SetProgress(float progress)
    {
        if (_progressFill == null) return;

        // Clamp 0-1
        float cleanProgress = Mathf.Clamp01(progress);

        // Convert to percentage
        _progressFill.style.width = Length.Percent(cleanProgress * 100f);
    }
}