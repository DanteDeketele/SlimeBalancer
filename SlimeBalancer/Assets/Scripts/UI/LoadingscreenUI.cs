using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class LoadingscreenUI : MonoBehaviour
{
    private VisualElement _loadingScreen;
    private TextElement _loadingText; // Use base TextElement to support Label/TextElement in UXML
    private VisualElement _progressFill;

    // Optional: Cache opacity for fade out later
    private VisualElement _root;

    public void Awake()
    {
        var doc = GetComponent<UIDocument>();
        _root = doc.rootVisualElement;

        _loadingScreen = _root.Q<VisualElement>("LoadingScreen");
        _loadingText = _root.Q<TextElement>("LoadingText"); // works for <Label> or <TextElement>

        // Only the fill is needed (its parent rail can be styled in USS)
        _progressFill = _root.Q<VisualElement>("ProgressFill");

        // Validate and log to help diagnose name/type mismatches
        if (_loadingText == null)
        {
            Debug.LogError("Loading Screen UI: Missing 'LoadingText' (Label/TextElement) in UXML.");
        }
        if (_progressFill == null)
        {
            Debug.LogError("Loading Screen UI: Missing 'ProgressFill' (VisualElement) in UXML.");
        }

        // Ensure width can shrink/grow by percentage (avoid flex-grow overriding width)
        if (_progressFill != null)
        {
            _progressFill.style.flexGrow = 0;
            _progressFill.style.minWidth = 0;
        }

        // Initialize
        SetProgress(0f);
        SetMessage("LOADING...");
    }

    public void SetMessage(string message)
    {
        if (_loadingText == null) return;
        _loadingText.text = (message ?? string.Empty).ToUpper(); // Force Uppercase for style
    }

    public void SetProgress(float progress)
    {
        if (_progressFill == null) return;

        // Clamp 0-1
        float cleanProgress = Mathf.Clamp01(progress);

        // Convert to percentage and assign explicit percent length
        _progressFill.style.width = new Length(cleanProgress * 100f, LengthUnit.Percent);
    }
}