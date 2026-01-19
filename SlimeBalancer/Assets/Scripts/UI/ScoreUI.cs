using UnityEngine;
using UnityEngine.UIElements;
public class ScoreUI : MonoBehaviour
{
    private Label scoreText;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        scoreText = root.Q<Label>("ScoreText");
        scoreText.text = "0";
    }
    public void SetScore(string score)
    {
        scoreText.text = score;
    }
}