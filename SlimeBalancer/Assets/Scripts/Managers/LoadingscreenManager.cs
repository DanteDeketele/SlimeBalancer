using UnityEngine;

public class LoadingscreenManager : BaseManager
{
    public GameObject LoadingScreenPrefab;
    private GameObject _loadingScreenInstance;

    private LoadingscreenUI _loadingScreenUI;

    public void ShowLoadingScreen()
    {
        if (_loadingScreenInstance == null)
        {
            _loadingScreenInstance = Instantiate(LoadingScreenPrefab);
            _loadingScreenUI = _loadingScreenInstance.GetComponent<LoadingscreenUI>();
        }
        _loadingScreenInstance.SetActive(true);
        _loadingScreenUI.SetProgress(0f);
        _loadingScreenUI.SetMessage("Loading...");
    }

    public void UpdateLoadingScreen(float progress, string message)
    {
        Debug.Log($"Loading Screen Update - Progress: {progress * 100}%, Message: {message}");
        if (_loadingScreenUI != null)
        {
            _loadingScreenUI.SetProgress(progress);
            _loadingScreenUI.SetMessage(message);
        }
    }

    public void HideLoadingScreen()
    {
        if (_loadingScreenInstance != null)
        {
            _loadingScreenInstance.SetActive(false);
        }
    }
}
