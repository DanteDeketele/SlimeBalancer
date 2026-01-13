using UnityEngine;
using UnityEngine.UIElements;
public class ScoreUI : MonoBehaviour
{
    private Label scoreText;

    private void Awake()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        scoreText = root.Q<Label>("ScoreText");
    }
    public void SetScore(string score)
    {
        scoreText.text = score;
    }
}