using UnityEngine;
using UnityEngine.UIElements;

public class CountdownUI : MonoBehaviour
{
    private Label _countdownLabel;
    private void Awake()
    {
        var uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;
        _countdownLabel = root.Q<Label>("CountdownLabel");
    }
    public void UpdateCountdown(int time)
    {
        _countdownLabel.text = time > 0 ? time.ToString() : "Go!";
    }
}
