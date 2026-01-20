using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    /// <summary>
    /// If set, the GameManager will skip directly to this game scene on load.
    /// </summary>
    public static string GameSceneNameToSkipTo = "";
    private BaseGame _currentGame;
    private GameData _currentGameData;
    public GameData CurrentGameData => _currentGameData;

    [System.Serializable]
    public class GameData
    {
        public string GameName;
        public string SceneName;
        public string genre;
        [TextArea]
        public string[] Explination;
        public Texture2D GameLogo;
    }

    [Header("Data")]
    [SerializeField] public List<GameData> AvailableGames;

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
    public static CountdownManager CountdownManager {
        get {
            if (_countdownManager == null)
            {
                Debug.LogError("CountdownManager is not initialized!");
            }
            return _countdownManager; 
        }
    }
    private static CountdownManager _countdownManager;
    public static GameManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject.transform.parent);
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
                case CountdownManager countdownManager:
                    _countdownManager = countdownManager;
                    _countdownManager.OnCountdownFinished.AddListener(OnStartGame);
                    break;
                default:
                    Debug.LogWarning($"Unknown manager type: {manager.GetType().Name}");
                    break;
            }
            manager.LogInitialization();
        }

        Debug.Log("GameManager initialized.");

        if (GameSceneNameToSkipTo != "")
        {
            LoadGame(GameSceneNameToSkipTo);
            GameSceneNameToSkipTo = "";
        }
        else
        {
            SceneManager.LoadScene(SceneManager.MainMenuSceneName);
        }
    }

    public void LoadGame(string sceneName)
    {
        Time.timeScale = 1f;
        StartCoroutine(LoadGameCoroutine(sceneName));
    }

    public IEnumerator LoadGameCoroutine(string sceneName)
    {
        InputManager.SetLightingEffect(InputManager.LightingEffect.Rainbow);
        yield return SceneManager.LoadSceneCoroutine(SceneManager.InfoSceneName);

        yield return new WaitForSeconds(2f);

        yield return SceneManager.LoadSceneCoroutine(sceneName);


        _currentGame = FindFirstObjectByType<BaseGame>();
        _currentGameData = AvailableGames.Find(g => g.SceneName == sceneName);
        if (_currentGame == null)
        {
            Debug.LogError("No BaseGame found in the loaded scene!");
            yield break;
        }
        yield return CountdownManager.CountdownCoroutine();
    }

    private void OnStartGame()
    {
        _currentGame?.StartGame();
        ScoreManager.ResetScore();
        ScoreManager.ShowPoints();
    }

    private void Update()
    {
        if (_currentGame != null)
        {
            if (_currentGame.IsGameActive)
                _currentGame.UpdateGame();
        }
    }

}
