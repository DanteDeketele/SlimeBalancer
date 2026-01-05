using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using USM = UnityEngine.SceneManagement.SceneManager;

public class SceneManager : BaseManager
{
    public string MainMenuSceneName = "MainMenu";
    private string activeSceneName;

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneFlow(sceneName));
    }

    private IEnumerator LoadSceneFlow(string sceneName)
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

        // Hide loading screen
        GameManager.LoadingscreenManager.HideLoadingScreen();
    }
}
