using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using USM = UnityEngine.SceneManagement.SceneManager;

public class SceneManager : BaseManager
{
    public string MainMenuSceneName = "MainMenu";
    public string EndScreen = "EndScene";
    public string InfoSceneName = "InfoScene";
    public string WaitingSceneName = "WaitingScene";
    public string SettingsSceneName = "SettingsMenu";
    private string activeSceneName;

    private List<string> _additiveScenes = new List<string>();

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName));
       
    }

    public IEnumerator LoadSceneCoroutine(string sceneName)
    {
        GameManager.LoadingscreenManager.ShowLoadingScreen();

        //Start unloading the current scene asynchronously
        if (!string.IsNullOrEmpty(activeSceneName))
        {
            AsyncOperation asyncUnload = USM.UnloadSceneAsync(activeSceneName);
            while (!asyncUnload.isDone)
            {
                float progress = Mathf.Clamp01(asyncUnload.progress / 0.9f);
                GameManager.LoadingscreenManager.UpdateLoadingScreen(progress, "Unloading...");
                yield return null;
            }
            
        }
        foreach (string additiveScene in _additiveScenes)
        {
            AsyncOperation asyncUnloadAdditive = USM.UnloadSceneAsync(additiveScene);
            while (!asyncUnloadAdditive.isDone)
            {
                float progress = Mathf.Clamp01(asyncUnloadAdditive.progress / 0.9f);
                GameManager.LoadingscreenManager.UpdateLoadingScreen(progress, "Unloading Additive...");
                yield return null;
            }
        }
        _additiveScenes.Clear();

        // Start loading the scene asynchronously
        AsyncOperation asyncLoad = USM.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            GameManager.LoadingscreenManager.UpdateLoadingScreen(progress, "Loading...");
            yield return null;
        }

        // Set the newly loaded scene as the active scene
        Scene loadedScene = USM.GetSceneByName(sceneName);
        USM.SetActiveScene(loadedScene);
        activeSceneName = sceneName;
        if(activeSceneName == "MainMenu" || activeSceneName == "InfoScene")
        {
            GameManager.SoundManager.PlaySound(GameManager.SoundManager.mainTheme, true, true);
        }
       
      
        // Hide loading screen
        GameManager.LoadingscreenManager.HideLoadingScreen();
        Debug.Log($"Scene '{sceneName}' loaded successfully.");
    }


    public void LoadSceneOnTop(string sceneName)
    {
        USM.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        _additiveScenes.Add(sceneName);
    }
}
