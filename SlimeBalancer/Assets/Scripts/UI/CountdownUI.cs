using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class CountdownUI : MonoBehaviour
{
    private Label _label;

    [Header("Settings")]
    [SerializeField] private float _slamDuration = 0.5f; // How fast it hits
    [SerializeField] private AnimationCurve _slamCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private void OnEnable()
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
            GameManager.SoundManager.PlaySound(GameManager.SoundManager.CountdownBeepSound);
            // 1. Setup the number
            _label.RemoveFromClassList("go-state"); // Ensure styling is correct
            _label.text = i.ToString();
            _label.style.opacity = 0;

            // 2. The "Slam" Animation
            float timer = 0f;
            while (timer < _slamDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / _slamDuration;

                // Evaluate curve for that "Impact" feel
                float val = Mathf.Lerp(0, 1f, _slamCurve.Evaluate(progress));

                _label.style.opacity = val;
                yield return null;
            }

            // 3. Wait on screen (The moment of anticipation)
            yield return new WaitForSeconds(0.6f);

            // 4. Vanish instantly before the next number
            _label.style.opacity = 0;
            yield return new WaitForSeconds(0.15f); // Short pause between numbers
        }

        GameManager.CountdownManager.OnCountdownFinished.Invoke();

        // --- GO! ---
        GameManager.SoundManager.PlaySound(GameManager.SoundManager.CountdownGoSound);
        _label.text = "GO!";
        _label.AddToClassList("go-state");
        _label.style.opacity = 1;

        yield return new WaitForSeconds(0.8f);

        // Cleanup
        _label.style.opacity = 0;

        yield return new WaitForSeconds(0.5f);
    }
}