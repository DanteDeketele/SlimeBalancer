using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

[RequireComponent(typeof(UIDocument))]
public class MainMenuUI : MonoBehaviour
{
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
        _submitAction.performed += ctx =>
        {
            var selectedGame = GameManager.Instance.AvailableGames[_selectedIndex];
            Debug.Log($"[MainMenuUI] Loading game: {selectedGame.GameName} (Scene: {selectedGame.SceneName})");
            // Here you would typically call your scene manager to load the scene
            GameManager.Instance.LoadGame(selectedGame.SceneName);
        };

        _navigateAction.Enable();
        _submitAction.Enable();
    }

    private void OnDisable() { _navigateAction.Disable(); _submitAction.Disable(); }

    private void CreateCards()
    {
        _carouselContainer.Clear();
        _cards.Clear();

        for (int i = 0; i < GameManager.Instance.AvailableGames.Count; i++)
        {
            var data = GameManager.Instance.AvailableGames[i];
            var card = new VisualElement();
            card.AddToClassList("game-card");
            if (data.GameLogo != null) card.style.backgroundImage = new StyleBackground(data.GameLogo);

            _carouselContainer.Add(card);
            _cards.Add(card);
        }
    }

    private void ChangeSelection(int dir)
    {
        int newIndex = Mathf.Clamp(_selectedIndex + dir, 0, GameManager.Instance.AvailableGames.Count - 1);
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
        _titleLabel.text = GameManager.Instance.AvailableGames[index].GameName;

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