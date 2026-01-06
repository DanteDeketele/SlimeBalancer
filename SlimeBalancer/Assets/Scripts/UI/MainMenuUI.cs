using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

[RequireComponent(typeof(UIDocument))]
public class MainMenuUI : MonoBehaviour
{
    [System.Serializable]
    public class GameData
    {
        public string GameName;
        public string SceneName;
        public Texture2D GameLogo;
    }

    [Header("Data")]
    [SerializeField] private List<GameData> _availableGames;

    [Header("Layout Settings")]
    [SerializeField] private float _normalWidth = 260f;
    [SerializeField] private float _selectedWidth = 360f + 8f;
    [SerializeField] private float _spacing = 16f;
    [SerializeField] private int _startPos = 60; // List starts 10% from left

    // Refs
    private VisualElement _root;
    private VisualElement _carouselContainer;
    private VisualElement _infoBox;
    private Label _titleLabel;
    private List<VisualElement> _cards = new List<VisualElement>();

    private int _selectedIndex = 0;

    // Inputs
    private InputAction _navigateAction;
    private InputAction _submitAction;

    private void Awake()
    {
        var uiDoc = GetComponent<UIDocument>();
        _root = uiDoc.rootVisualElement;

        _carouselContainer = _root.Q<VisualElement>("CarouselContainer");
        _infoBox = _root.Q<VisualElement>("InfoBox");
        _titleLabel = _root.Q<Label>("GameTitle");

        SetupInputs();
        CreateCards();

        // Wait for layout calculation
        _root.RegisterCallback<GeometryChangedEvent>(OnLayoutReady);
    }

    private void OnLayoutReady(GeometryChangedEvent evt)
    {
        _root.UnregisterCallback<GeometryChangedEvent>(OnLayoutReady);
        UpdateSelection(0);
    }

    private void SetupInputs()
    {
        _navigateAction = new InputAction("Navigate", type: InputActionType.Value, expectedControlType: "Vector2");
        _navigateAction.AddCompositeBinding("2DVector")
            .With("Left", "<Keyboard>/a").With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/d").With("Right", "<Keyboard>/rightArrow");

        _navigateAction.performed += ctx => {
            float x = ctx.ReadValue<Vector2>().x;
            if (x < -0.5f) ChangeSelection(-1);
            else if (x > 0.5f) ChangeSelection(1);
        };

        _submitAction = new InputAction("Submit", type: InputActionType.Button);
        _submitAction.AddBinding("<Keyboard>/enter");
        _submitAction.performed += ctx => Debug.Log($"Load: {_availableGames[_selectedIndex].SceneName}");

        _navigateAction.Enable();
        _submitAction.Enable();
    }

    private void OnDisable() { _navigateAction.Disable(); _submitAction.Disable(); }

    private void CreateCards()
    {
        _carouselContainer.Clear();
        _cards.Clear();

        for (int i = 0; i < _availableGames.Count; i++)
        {
            var data = _availableGames[i];
            var card = new VisualElement();
            card.AddToClassList("game-card");
            if (data.GameLogo != null) card.style.backgroundImage = new StyleBackground(data.GameLogo);

            _carouselContainer.Add(card);
            _cards.Add(card);
        }
    }

    private void ChangeSelection(int dir)
    {
        int newIndex = Mathf.Clamp(_selectedIndex + dir, 0, _availableGames.Count - 1);
        if (newIndex != _selectedIndex) UpdateSelection(newIndex);
    }

    private void UpdateSelection(int index)
    {
        _selectedIndex = index;

        // 1. Visuals
        for (int i = 0; i < _cards.Count; i++)
        {
            if (i == index) _cards[i].AddToClassList("game-card--selected");
            else _cards[i].RemoveFromClassList("game-card--selected");
        }

        // 2. Text
        _titleLabel.text = _availableGames[index].GameName;

        // 3. Carousel Position (Left to Right)
        float screenWidth = _root.layout.width;
        if (float.IsNaN(screenWidth) || screenWidth < 1) screenWidth = 1920f;

        float anchorPos = _startPos;

        // Calculate shift: Sum of width+spacing for all PREVIOUS items
        float shift = index * (_normalWidth + _spacing);

        // Move the carousel
        _carouselContainer.style.left = anchorPos - shift;

        // 4. Info Box Position (Horizontal Lock)
        // It should start at: Anchor + SelectedCardWidth + Gap
        float textLeftPos = anchorPos + _selectedWidth + _spacing;
        _infoBox.style.left = textLeftPos;
    }
}