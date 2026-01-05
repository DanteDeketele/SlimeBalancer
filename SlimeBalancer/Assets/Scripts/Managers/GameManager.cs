using UnityEngine;

public class GameManager : MonoBehaviour
{
    public ScoreManager ScoreManager 
    {
        get {
            if (_scoreManager == null)
            {
                Debug.LogError("ScoreManager is not initialized!");
            }
            return _scoreManager; 
        }
    }
    private ScoreManager _scoreManager;
    public InputManager InputManager
    {
        get {
            if (_inputManager == null)
            {
                Debug.LogError("InputManager is not initialized!");
            }
            return _inputManager; 
        }
    }
    private InputManager _inputManager;
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
                default:
                    Debug.LogWarning($"Unknown manager type: {manager.GetType().Name}");
                    break;
            }
            manager.LogInitialization();
        }

        Debug.Log("GameManager initialized.");
    }

}
