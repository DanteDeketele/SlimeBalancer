using UnityEngine;
using UnityEngine.UIElements;

public class WaitingUI : MonoBehaviour
{
    VisualElement root;

    private void OnEnable()
    {
        GameManager.InputManager.OnDown.AddListener(LoadMainMenu);
    }

    private void OnDisable()
    {
        GameManager.InputManager.OnDown.RemoveListener(LoadMainMenu);
    }

    private void LoadMainMenu()
    {
        GameManager.SceneManager.LoadScene(GameManager.SceneManager.MainMenuSceneName);
    }
}