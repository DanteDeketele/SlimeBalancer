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
            _countdownUIInstance = Instantiate(CountdownUI, transform).GetComponent<CountdownUI>();
        }
        _countdownUIInstance.gameObject.SetActive(true);

        for (int i = CountdownTime; i > 0; i--)
        {
            Debug.Log(i);
            _countdownUIInstance.UpdateCountdown(i);
            yield return new WaitForSeconds(1f);
        }
        Debug.Log("Go!");
        _countdownUIInstance.UpdateCountdown(0);
        yield return new WaitForSeconds(1f);


        _countdownUIInstance.gameObject.SetActive(false);
    }
}