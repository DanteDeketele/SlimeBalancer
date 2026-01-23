using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class ConnectingUI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    VisualElement root;
    VisualElement ConnectionColor;
    Label BoardOnlineLabel;
    void Start()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        ConnectionColor = root.Q<VisualElement>("ConnectionColor"); 
        BoardOnlineLabel = root.Q<Label>("BoardOnlineLabel");
        
    }

    private void Update()
    {
        if (GameManager.InputManager.IsConnected)
        {
            string colorHex = "30d5c8";
            Color color;
            ColorUtility.TryParseHtmlString("#" + colorHex, out color);
            ConnectionColor.style.backgroundColor = new StyleColor(color);
            BoardOnlineLabel.text = "Bord Online";
            StartCoroutine(Wait(2f));
        }

        // if space is pressed continue without connection saying "Continuing without board connection" and make it orange
        if (Input.GetKeyDown(KeyCode.Space))
        {
            string colorHex = "ffa500";
            Color color;
            ColorUtility.TryParseHtmlString("#" + colorHex, out color);
            ConnectionColor.style.backgroundColor = new StyleColor(color);
            BoardOnlineLabel.text = "Continuing without board connection";
            StartCoroutine(Wait(2f));
        }
    }

    private IEnumerator Wait(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        GameManager.SceneManager.LoadScene(GameManager.SceneManager.WaitingSceneName);
    }
}
