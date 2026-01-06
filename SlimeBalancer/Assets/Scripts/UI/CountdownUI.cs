using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class CountdownUI : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Time in seconds for the countdown to start from.")]
    [SerializeField] private int _startCount = 3;

    [Tooltip("Duration of the beat animation in seconds.")]
    [SerializeField] private float _pulseDuration = 0.1f;

    // UI References
    private Label _countdownLabel;
    private const string LabelName = "CountdownLabel";

    // USS Class Names
    private const string PulseClassName = "pulse";
    private const string GoStateClassName = "go-state";

    /// <summary>
    /// Initialization of UI elements from the UIDocument.
    /// </summary>
    private void Awake()
    {
        var uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        _countdownLabel = root.Q<Label>(LabelName);

        if (_countdownLabel == null)
        {
            Debug.LogError($"[CountdownUI] Could not find Label with name '{LabelName}'");
        }
        else
        {
            // clear text on start
            _countdownLabel.text = "";
        }
    }

    /// <summary>
    /// For testing purposes: Starts the countdown when the game begins.
    /// </summary>
    private void Start()
    {
        StartCoroutine(RunCountdownRoutine());
    }

    /// <summary>
    /// Updates the label text and triggers a visual pulse effect.
    /// </summary>
    /// <param name="time">The current time remaining.</param>
    public void UpdateCountdown(int time)
    {
        if (_countdownLabel == null) return;

        // Update Text
        if (time > 0)
        {
            _countdownLabel.text = time.ToString();
        }
        else
        {
            _countdownLabel.text = "Go!";
            _countdownLabel.AddToClassList(GoStateClassName);
        }

        // Trigger Animation
        StartCoroutine(PulseEffect());
    }

    /// <summary>
    /// Coroutine that handles the timing logic.
    /// </summary>
    private IEnumerator RunCountdownRoutine()
    {
        int currentTime = _startCount;

        while (currentTime >= 0)
        {
            UpdateCountdown(currentTime);
            yield return new WaitForSeconds(1f);
            currentTime--;
        }

        // Optional: Hide after "Go!" finishes
        yield return new WaitForSeconds(1f);
        _countdownLabel.text = "";
    }

    /// <summary>
    /// Toggles the USS class to create a scaling animation effect.
    /// </summary>
    private IEnumerator PulseEffect()
    {
        // Add class to scale up
        _countdownLabel.AddToClassList(PulseClassName);

        yield return new WaitForSeconds(_pulseDuration);

        // Remove class to scale back down (transition handled by USS)
        _countdownLabel.RemoveFromClassList(PulseClassName);
    }
}