using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class CountdownUI : MonoBehaviour
{
    private Label _label;

    [Header("Settings")]
    [SerializeField] private float _startScale = 3.5f; // Start HUGE
    [SerializeField] private float _slamDuration = 0.25f; // How fast it hits
    [SerializeField] private AnimationCurve _slamCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private void Awake()
    {
        var doc = GetComponent<UIDocument>();
        _label = doc.rootVisualElement.Q<Label>("CountdownLabel");
        _label.text = ""; // Hide on start
    }

    public IEnumerator StartCountdown()
    {
        // 3... 2... 1...
        GameManager.InputManager.SetLightingEffect(InputManager.LightingEffect.Rainbow);
        for (int i = 3; i > 0; i--)
        {
            // 1. Setup the number
            _label.RemoveFromClassList("go-state"); // Ensure styling is correct
            _label.text = i.ToString();
            _label.style.opacity = 1;

            // 2. The "Slam" Animation
            // We manually animate scale from _startScale down to 1
            float timer = 0f;
            while (timer < _slamDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / _slamDuration;

                // Evaluate curve for that "Impact" feel
                float scaleVal = Mathf.Lerp(_startScale, 1f, _slamCurve.Evaluate(progress));

                _label.style.scale = new Scale(new Vector2(scaleVal, scaleVal));
                yield return null;
            }

            // Ensure it lands perfectly on 1
            _label.style.scale = new Scale(Vector2.one);

            // 3. Wait on screen (The moment of anticipation)
            yield return new WaitForSeconds(0.6f);

            // 4. Vanish instantly before the next number
            _label.style.opacity = 0;
            yield return new WaitForSeconds(0.15f); // Short pause between numbers
        }

        GameManager.CountdownManager.OnCountdownFinished.Invoke();

        // --- GO! ---
        _label.text = "GO!";
        _label.AddToClassList("go-state");
        _label.style.opacity = 1;
        _label.style.scale = new Scale(new Vector2(1.5f, 1.5f)); // Start slightly bigger

        // Optional: Shake the 'GO' text slightly
        float goTimer = 0;
        while (goTimer < 1.0f)
        {
            goTimer += Time.deltaTime;
            // Simple wobble
            float wobble = Mathf.Sin(Time.time * 50) * 5f;
            _label.style.rotate = new Rotate(wobble);
            yield return null;
        }

        // Cleanup
        _label.style.opacity = 0;
    }
}