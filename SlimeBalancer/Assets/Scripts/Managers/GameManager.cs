using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static ScoreManager ScoreManager 
    {
        get {
            if (_scoreManager == null)
            {
                Debug.LogError("ScoreManager is not initialized!");
            }
            return _scoreManager; 
        }
    }
    private static ScoreManager _scoreManager;
    public static InputManager InputManager
    {
        get {
            if (_inputManager == null)
            {
                Debug.LogError("InputManager is not initialized!");
            }
            return _inputManager; 
        }
    }
    private static InputManager _inputManager;
    public static LoadingscreenManager LoadingscreenManager
    {
        get {
            if (_loadingscreenManager == null)
            {
                Debug.LogError("LoadingscreenManager is not initialized!");
            }
            return _loadingscreenManager; 
        }
    }
    private static LoadingscreenManager _loadingscreenManager;
    public static SceneManager SceneManager
    {
        get {
            if (_sceneManager == null)
            {
                Debug.LogError("SceneManager is not initialized!");
            }
            return _sceneManager; 
        }
    }
    private static SceneManager _sceneManager;
    public static GameManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        Debug.Log("GameManager initializing...");

        BaseManager[] Managers = FindObjectsByType<BaseManager>(FindObjectsSortMode.None);
        foreach (BaseManager manager in Managers)
        {
            switch (manager)
            {
                case ScoreManager scoreManager:
                    _scoreManager = scoreManager;
                    break;
                case InputManager inputManager:
                    _inputManager = inputManager;
                    break;
                case LoadingscreenManager loadingscreenManager:
                    _loadingscreenManager = loadingscreenManager;
                    break;
                case SceneManager sceneManager:
                    _sceneManager = sceneManager;
                    break;
                default:
                    Debug.LogWarning($"Unknown manager type: {manager.GetType().Name}");
                    break;
            }
            manager.LogInitialization();
        }

        Debug.Log("GameManager initialized.");

        SceneManager.LoadScene(SceneManager.MainMenuSceneName);
    }

}
