using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
public class CountdownManager : BaseManager
{
    public int CountdownTime = 3;
    public UnityEvent OnCountdownFinished;
    public GameObject CountdownUI;
    private GameObject _countdownUIInstance;
    public void StartCountdown()
    {
        StartCoroutine(CountdownCoroutine());
    }
    public IEnumerator CountdownCoroutine()
    {
        if (_countdownUIInstance == null)
        {
            _countdownUIInstance = Instantiate(CountdownUI, transform);
        }
        _countdownUIInstance.SetActive(true);

        for (int i = CountdownTime; i > 0; i--)
        {
            Debug.Log(i);
            yield return new WaitForSeconds(1f);
        }
        Debug.Log("Go!");

        _countdownUIInstance.SetActive(false);
    }
}