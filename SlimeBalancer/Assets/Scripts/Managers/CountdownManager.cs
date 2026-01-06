using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
public class CountdownManager : BaseManager
{
    public int CountdownTime = 3;
    public UnityEvent OnCountdownFinished;
    public GameObject CountdownUI;
    private CountdownUI _countdownUIInstance;
    public void StartCountdown()
    {
        StartCoroutine(CountdownCoroutine());
    }
    public IEnumerator CountdownCoroutine()
    {
        if (_countdownUIInstance == null)
        {
            GameObject uiInstance = Instantiate(CountdownUI);
            _countdownUIInstance = uiInstance.GetComponent<CountdownUI>();
        }
        _countdownUIInstance.gameObject.SetActive(true);

        yield return _countdownUIInstance.StartCountdown();
        _countdownUIInstance.gameObject.SetActive(false);
    }
}