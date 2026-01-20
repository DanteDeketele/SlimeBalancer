using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class MainMenuUI : MonoBehaviour
{
    public VisualTreeAsset gameContainerTemplate;
    private VisualElement root;
    private int selectedGameIndex = 0;

    private VisualElement gameListContainer;
    private VisualElement boardOnlineCircle;
    private Label boardOnlineLabel;

    float timer = 0f;
    public float scrollDelay = 0.5f;
    float _scrollPosition = 0f;
    float _startScrollPosition = 0f;
    float containerSize = 0f;

    int actualScreenWidth = 0;

    public void Awake()
    {
        GameManager.InputManager.SetLightingEffect(InputManager.LightingEffect.Rainbow);
        var uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        boardOnlineCircle = root.Q<VisualElement>("BoardOnlineCircle");
        boardOnlineLabel = root.Q<Label>("BoardOnlineLabel");

        GameManager.GameData[] games = GameManager.Instance.AvailableGames.ToArray();

        gameListContainer = root.Q<VisualElement>("Carousel");
        foreach (var gameData in games)
        {
            var gameEntry = gameContainerTemplate.CloneTree();
            gameEntry.Q<Label>("game-title").text = gameData.GameName;
            gameEntry.Q<Label>("game-genre").text = "Test";
            gameEntry.Q<VisualElement>("game-icon").style.backgroundImage = new StyleBackground(gameData.GameLogo);
            gameListContainer.Add(gameEntry);
        }

        var icon = gameListContainer[1].Q<VisualElement>("game-icon");

        // Register a callback to run ONLY when the layout geometry is calculated
        icon.RegisterCallback<GeometryChangedEvent>(OnGeometryCalculated);

        UpdateSelectedGame();

        GameManager.InputManager.OnLeft.AddListener(() =>
        {
            selectedGameIndex = Mathf.Max(0, selectedGameIndex - 1);
            UpdateSelectedGame();
            BeginScroll();
        });

        GameManager.InputManager.OnRight.AddListener(() =>
        {
            selectedGameIndex = Mathf.Min(games.Length - 1, selectedGameIndex + 1);
            UpdateSelectedGame();
            BeginScroll();
        });

        GameManager.InputManager.OnDown.AddListener(() =>
        {
            var selectedGame = games[selectedGameIndex];
            Debug.Log($"Selected game: {selectedGame.GameName}");
            GameManager.Instance.LoadGame(selectedGame.SceneName);
            // remove listeners to prevent multiple loads
            GameManager.InputManager.OnLeft.RemoveAllListeners();
            GameManager.InputManager.OnRight.RemoveAllListeners();
            GameManager.InputManager.OnDown.RemoveAllListeners();
        });
    }

    void OnGeometryCalculated(GeometryChangedEvent evt = null)
    {
        float actualWidth = 0f;
        if (evt != null)
        {
            // 1. Get the width directly from the event (it's faster and guaranteed not to be NaN)
            actualWidth = evt.newRect.width;
            Debug.Log($"Calculated Width: {actualWidth}");
        }else
        {
            // Fallback: Get the width from the element directly
            actualWidth = gameListContainer[1].Q<VisualElement>("GameContainer").resolvedStyle.width;
            Debug.Log($"Calculated Width (Fallback): {actualWidth}");
        }

        // 2. Get the margin from the container
        float marginRight = gameListContainer[1].Q<VisualElement>("GameContainer").resolvedStyle.marginRight;
        Debug.Log($"Calculated Margin Right: {marginRight}");

        // Safety check: specific layout edge cases might still leave margin as NaN
        if (float.IsNaN(marginRight)) marginRight = 0;

        float totalSize = actualWidth + marginRight;
        Debug.Log($"Final Container Size: {totalSize}");
        containerSize = totalSize;

        float bigSize = gameListContainer[0].Q<VisualElement>("GameContainer").resolvedStyle.width;
        Debug.Log($"Big Container Width: {bigSize}");

        //screen width is not the actual screen width in pixels due to scaling based reference screen size of the ui in panel settings
        PanelSettings panelSettings = GetComponent<UIDocument>().panelSettings;
        float screenWidth = 0f;

        if (panelSettings.scaleMode == PanelScaleMode.ConstantPixelSize)
        {
            screenWidth = Screen.width;
        }
        else if (panelSettings.scaleMode == PanelScaleMode.ScaleWithScreenSize)
        {
            float match = panelSettings.match;
            // we use height so match is 1
            float referenceWidth = panelSettings.referenceResolution.x;
            float referenceHeight = panelSettings.referenceResolution.y;

            float logWidth = Screen.width / referenceWidth;
            float logHeight = Screen.height / referenceHeight;

            float logWeightedAverage = Mathf.Lerp(logWidth, logHeight, match);
            float scaleFactor = logWeightedAverage;
            screenWidth = Screen.width / scaleFactor;
        }


        // give the carousel a left border of 50% - 50% of the container size to center the selected item
        float leftBorder = (screenWidth / 2f) - (bigSize / 2f);
        Debug.Log($"Setting left border to: {leftBorder}px, Screen Width: {screenWidth}px");
        gameListContainer.style.paddingLeft = leftBorder;

        // 3. IMPORTANT: Unregister to prevent this from running every time the UI updates
        var element = evt.target as VisualElement;
        element.UnregisterCallback<GeometryChangedEvent>(OnGeometryCalculated);
    }

    private void UpdateSelectedGame()
    {
        GameManager.GameData[] games = GameManager.Instance.AvailableGames.ToArray();
        for (int i = 0; i < gameListContainer.childCount; i++)
        {
            var gameEntry = gameListContainer[i];
            if (i == selectedGameIndex)
            {
                gameEntry.AddToClassList("selected");
            }
            else
            {
                gameEntry.RemoveFromClassList("selected");
            }
        }
    }

    private void BeginScroll()
    {
        // Set the start position to the current scroll position and start the timer
        _startScrollPosition = _scrollPosition;
        timer = scrollDelay;
    }

    private void Update()
    {
        float targetPosition = selectedGameIndex * containerSize; // Assuming each game entry is 200px wide + 20px margin

        if (timer > 0f)
        {
            timer -= Time.deltaTime;
            float t = Mathf.Clamp01(1f - (timer / scrollDelay));
            _scrollPosition = Mathf.Lerp(_startScrollPosition, targetPosition, t);
            gameListContainer.style.translate = new StyleTranslate(new Translate(-_scrollPosition, 0f));

            // When finished, ensure final position is exact
            if (timer <= 0f)
            {
                _scrollPosition = targetPosition;
                gameListContainer.style.translate = new StyleTranslate(new Translate(-_scrollPosition, 0f));
            }
        }
        else
        {
            // Keep container in sync when idle
            _scrollPosition = targetPosition;
            gameListContainer.style.translate = new StyleTranslate(new Translate(-_scrollPosition, 0f));
        }


        if (actualScreenWidth != Screen.width)
        {
            actualScreenWidth = Screen.width;

            OnGeometryCalculated();
        }

        if (GameManager.InputManager.IsConnected)
        {
            boardOnlineCircle.style.backgroundColor = new StyleColor(Color.green);
            boardOnlineLabel.text = GameManager.InputManager.BatteryLevel.ToString() + "%";
        }
        else
        {
            boardOnlineCircle.style.backgroundColor = new StyleColor(Color.red);
            boardOnlineLabel.text = "Bord Offline";
        }
    }
}