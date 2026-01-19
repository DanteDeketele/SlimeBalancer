using UnityEngine;
using UnityEngine.UIElements;

public class InfoUI : MonoBehaviour
{
    private VisualElement root;
    private Label infoLabel;
    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;
        infoLabel = root.Q<Label>("InfoLabel");
    }
    public void SetInfoText(string text)
    {
        infoLabel.text = text;
    }
}