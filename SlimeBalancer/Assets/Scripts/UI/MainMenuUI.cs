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
    public Texture2D SettingsIcon;
    public Texture2D ControlDownIcon;
    private VisualElement BatteryIcon;
    private Label BatteryPercentage;

    float timer = 0f;
    public float scrollDelay = 0.5f;
    float _scrollPosition = 0f;
    float _startScrollPosition = 0f;
    float containerSize = 0f;

    int actualScreenWidth = 0;

    public void Awake()
    {
        selectedGameIndex = GameManager.LastSelectedGameIndex;
        GameManager.SoundManager.StopAllMusic();
        GameManager.InputManager.SetLightingEffect(InputManager.LightingEffect.Rainbow);
        var uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        boardOnlineCircle = root.Q<VisualElement>("BoardOnlineCircle");
        boardOnlineLabel = root.Q<Label>("BoardOnlineLabel");



        BatteryIcon = root.Q<VisualElement>("BatteryIcon");
        BatteryPercentage = root.Q<Label>("BatteryPercentage");


        GameManager.GameData[] games = GameManager.Instance.AvailableGames.ToArray();

        gameListContainer = root.Q<VisualElement>("Carousel");
        foreach (var gameData in games)
        {
            var gameEntry = gameContainerTemplate.CloneTree();
            gameEntry.Q<Label>("game-title").text = gameData.GameName;
            gameEntry.Q<Label>("game-genre").text = gameData.genre;
            gameEntry.Q<VisualElement>("game-icon").style.backgroundImage = new StyleBackground(gameData.GameLogo);
            gameListContainer.Add(gameEntry);
        }

        // Add Settings entry
        var settingsEntry = gameContainerTemplate.CloneTree();
        settingsEntry.Q<Label>("game-title").text = "Instellingen";
        settingsEntry.Q<Label>("game-genre").text = "Instellingen";
        settingsEntry.Q<Label>("game-genre").style.color = new StyleColor(Color.white);
        // solid color for settings icon background
        string colorHex = "30D5C8";
        Color color;
        ColorUtility.TryParseHtmlString("#" + colorHex, out color);
        settingsEntry.Q<VisualElement>("game-icon").style.backgroundColor = new StyleColor(color);
        settingsEntry.Q<VisualElement>("game-icon").style.backgroundImage = new StyleBackground(); // clear any existing background image
        settingsEntry.Q<VisualElement>("game-icon").style.justifyContent = Justify.Center;
        // Add Icon with 70% width and height into the game-icon element
        VisualElement iconContainer = new VisualElement();
        iconContainer.style.width = Length.Percent(80);
        iconContainer.style.height = Length.Percent(80);
        iconContainer.style.alignSelf = Align.Center;
        iconContainer.style.backgroundImage = new StyleBackground(SettingsIcon);
        settingsEntry.Q<VisualElement>("game-icon").Add(iconContainer);

        gameListContainer.Add(settingsEntry);

        var icon = gameListContainer[1].Q<VisualElement>("game-icon");

        // Register a callback to run ONLY when the layout geometry is calculated
        icon.RegisterCallback<GeometryChangedEvent>(OnGeometryCalculated);

        UpdateSelectedGame();

        GameManager.InputManager.OnLeft.AddListener(() =>
        {
            if (selectedGameIndex > 0)
            {
                selectedGameIndex = Mathf.Max(0, selectedGameIndex - 1);
                GameManager.LastSelectedGameIndex = selectedGameIndex;
                GameManager.SoundManager.PlaySound(GameManager.SoundManager.UISelectSound);
                UpdateSelectedGame();
                BeginScroll();
            }
        });

        GameManager.InputManager.OnRight.AddListener(() =>
        {
            if (selectedGameIndex < games.Length)
            {
                selectedGameIndex = Mathf.Min(games.Length - 1 + 1, selectedGameIndex + 1);
                GameManager.LastSelectedGameIndex = selectedGameIndex;
                GameManager.SoundManager.PlaySound(GameManager.SoundManager.UISelectSound);
                UpdateSelectedGame();
                BeginScroll();
            }
        });

        GameManager.InputManager.OnDown.AddListener(() =>
        {
            if (selectedGameIndex == games.Length)
            {
                Debug.Log("Opening Settings Menu...");
                // Open Settings Menu
                GameManager.SoundManager.PlaySound(GameManager.SoundManager.GameSelectSound);

                GameManager.SceneManager.LoadScene(GameManager.SceneManager.SettingsSceneName);
                // remove listeners to prevent multiple loads
                GameManager.InputManager.OnLeft.RemoveAllListeners();
                GameManager.InputManager.OnRight.RemoveAllListeners();
                GameManager.InputManager.OnDown.RemoveAllListeners();
                return;
            }
            else
            {
                GameManager.SoundManager.PlaySound(GameManager.SoundManager.GameSelectSound);
                var selectedGame = games[selectedGameIndex];
                Debug.Log($"Selected game: {selectedGame.GameName}");
                GameManager.Instance.LoadInfo(selectedGame.SceneName);
            }
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
        }
        else
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

                if (i != games.Length)
                {
                    VisualElement scoreSection = gameEntry.Q<VisualElement>("game-score-section");
                    if (scoreSection != null)
                    {
                        scoreSection.style.opacity = 1f;
                    }
                    Label score = gameEntry.Q<Label>("game-score");
                    if (score != null)
                    {
                        score.text = GameManager.ScoreManager.GetHighScore(games[i].SceneName).ToString();
                    }
                }else
                {
                    VisualElement scoreSection = gameEntry.Q<VisualElement>("game-score-section");
                    if (scoreSection != null)
                    {
                        scoreSection.style.opacity = 0f;
                    }
                }

                // Add a position abstract ino label next to the icon alligned to the top
                VisualElement container = gameEntry.Q<VisualElement>("InfoContainer");
                if (container != null)
                {
                    for (int j = 0; j < container.childCount; j++)
                    {
                        var child = container[j];
                        child.style.opacity = 1f;
                    }
                }
                else
                {
                    container = new VisualElement();
                    container.name = "InfoContainer";
                    // info container should be positioned next to the game entry
                    container.style.position = Position.Absolute;
                    container.style.top = 0;
                    container.style.left = Length.Percent(100);
                    container.style.width = Length.Pixels(240);
                    container.style.flexGrow = 0;
                    container.style.marginLeft = Length.Pixels(-20);
                    gameEntry.Add(container);

                    Label instructionLabel = new Label("Druk om te spelen");
                    instructionLabel.name = "InstructionLabel";
                    instructionLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    instructionLabel.style.fontSize = 20;
                    instructionLabel.style.color = new StyleColor(Color.black);
                    instructionLabel.style.width = Length.Percent(100);
                    instructionLabel.style.unityTextAlign = TextAnchor.UpperCenter;
                    instructionLabel.style.whiteSpace = WhiteSpace.Normal;
                    instructionLabel.style.marginBottom = Length.Pixels(0);
                    instructionLabel.style.marginLeft = Length.Pixels(0);
                    instructionLabel.style.marginRight = Length.Pixels(0);
                    instructionLabel.style.paddingRight = Length.Pixels(0);
                    instructionLabel.style.paddingLeft = Length.Pixels(0);
                    instructionLabel.style.paddingBottom = Length.Pixels(0);

                    if (i == games.Length)
                    {
                        instructionLabel.text = "Druk om instellingen te openen";
                    }

                    container.Add(instructionLabel);

                    VisualElement ControlIcon = new VisualElement();
                    ControlIcon.name = "ControlIcon";
                    ControlIcon.style.width = Length.Pixels(70);
                    ControlIcon.style.height = Length.Pixels(50);
                    ControlIcon.style.backgroundImage = new StyleBackground(ControlDownIcon);
                    ControlIcon.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Contain));
                    ControlIcon.style.alignSelf = Align.Center;

                    container.Add(ControlIcon);


                    // add transition for opacity
                    for (int j = 0; j < container.childCount; j++)
                    {
                        var child = container[j];
                        child.style.opacity = 1f;
                    }
                }

            }
            else
            {
                gameEntry.RemoveFromClassList("selected");

                VisualElement scoreSection = gameEntry.Q<VisualElement>("game-score-section");
                if (scoreSection != null)
                {
                    scoreSection.style.opacity = 0f;
                }

                // Set the opacity of the position abstract info text and backgrounds to 0
                VisualElement container = gameEntry.Q<VisualElement>("InfoContainer");
                if (container != null)
                {
                    for (int j = 0; j < container.childCount; j++)
                    {
                        var child = container[j];
                        child.style.opacity = 0f;
                    }
                }
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
            string colorHex = "30d5c8";
            Color color;
            ColorUtility.TryParseHtmlString("#" + colorHex, out color);
            boardOnlineCircle.style.backgroundColor = new StyleColor(color);
            boardOnlineLabel.text = "Bord Online";
            BatteryPercentage.text = GameManager.InputManager.BatteryLevel.ToString() + "%";
            int icons = GameManager.Instance.BatteryIcons.Length;
            int level = Mathf.Clamp(GameManager.InputManager.BatteryLevel, 0, 100);
            int index = Mathf.FloorToInt((level / 100f) * (icons - 1));
            Texture2D batteryLowIcon = GameManager.Instance.BatteryIcons[index];
            BatteryIcon.style.backgroundImage = new StyleBackground(batteryLowIcon);
        }
        else
        {
            string colorHex = "ff5b63";
            Color color;
            ColorUtility.TryParseHtmlString("#" + colorHex, out color);
            boardOnlineCircle.style.backgroundColor = new StyleColor(color);
            boardOnlineLabel.text = "Bord Offline";
            BatteryPercentage.text = "--%";
            BatteryIcon.style.backgroundImage = GameManager.Instance.BatteryIcons[0];
        }
    }
}