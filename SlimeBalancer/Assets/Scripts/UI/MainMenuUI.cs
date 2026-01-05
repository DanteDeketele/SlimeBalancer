using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuUI : MonoBehaviour
{
    private VisualElement SkiButton;

    public void Awake()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        SkiButton = root.Q<VisualElement>("SkiButton");
        SkiButton.RegisterCallback<ClickEvent>(ev => OnSkiButtonClicked());
    }

    private void OnSkiButtonClicked()
    {
        Debug.Log("Ski Button Clicked!");
        GameManager.Instance.LoadGame("Skie");
    }
}
