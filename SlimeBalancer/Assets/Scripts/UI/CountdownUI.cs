using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class CountdownUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _startCount = 3;
    [SerializeField] private float _beatDuration = 1.0f; // Time between numbers

    // UI References
    private Label _countdownLabel;

    // Class Names
    private const string ClassAnimateIn = "animate-in";
    private const string ClassGoState = "go-state";

    private void Awake()
    {
        var uiDocument = GetComponent<UIDocument>();
        _countdownLabel = uiDocument.rootVisualElement.Q<Label>("CountdownLabel");

        // Ensure label is clean on start
        _countdownLabel.text = "";
        _countdownLabel.RemoveFromClassList(ClassAnimateIn);
    }

    public IEnumerator StartCountdown()
    {
        int count = _startCount;

        while (count > 0)
        {
            // 1. Reset State (Shrink out)
            _countdownLabel.RemoveFromClassList(ClassAnimateIn);
            _countdownLabel.text = count.ToString();

            // Wait a tiny frame for the CSS to register the "Shrink" state
            yield return null;

            // 2. Apply Random/Alternating Tilt
            // "3" tilts left, "2" tilts right, etc.
            float tiltAngle = (count % 2 == 0) ? 15f : -15f;
            _countdownLabel.style.rotate = new Rotate(tiltAngle);

            // 3. Trigger "Pop In" Animation
            _countdownLabel.AddToClassList(ClassAnimateIn);

            // Optional: Play Sound Effect here (e.g., UI_Pop.wav)

            // 4. Wait for the beat
            yield return new WaitForSeconds(_beatDuration);

            count--;
        }

        // --- GO STATE ---

        GameManager.CountdownManager.OnCountdownFinished?.Invoke();

        // Reset for the "GO" pop
        _countdownLabel.RemoveFromClassList(ClassAnimateIn);
        yield return null;

        _countdownLabel.text = "GO!";
        _countdownLabel.style.rotate = new Rotate(0); // Straighten it out

        _countdownLabel.AddToClassList(ClassAnimateIn);
        _countdownLabel.AddToClassList(ClassGoState);

        // Optional: Screen Shake could go here

        // Wait, then cleanup
        yield return new WaitForSeconds(1.5f);
        _countdownLabel.text = "";
        _countdownLabel.RemoveFromClassList(ClassGoState);
        _countdownLabel.RemoveFromClassList(ClassAnimateIn);
    }
}