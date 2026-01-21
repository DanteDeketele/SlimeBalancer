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
        Debug.Log("playing sound and loading main menu");
        
        GameManager.SoundManager.PlaySound(GameManager.SoundManager.UISelectSound);
        GameManager.SceneManager.LoadScene(GameManager.SceneManager.MainMenuSceneName);
        
    }
}