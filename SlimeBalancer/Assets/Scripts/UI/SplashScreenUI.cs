using System.Collections;
using UnityEngine;

public class SplashScreenUI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(Wait(3f));
    }

    private IEnumerator Wait(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        GameManager.SceneManager.LoadScene(GameManager.SceneManager.ConnectingSceneName);
    }
}
