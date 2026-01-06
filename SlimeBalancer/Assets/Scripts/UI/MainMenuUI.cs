using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

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

    [Header("Layout Settings (Must match USS)")]
    [Tooltip("Width of an unselected card")]
    [SerializeField] private float _normalWidth = 220f;

    [Tooltip("Width of the selected card")]
    [SerializeField] private float _selectedWidth = 340f; // Not used in math, but good for ref

    [Tooltip("Gap between unselected cards")]
    [SerializeField] private float _spacing = 12f;

    [Tooltip("Screen position X where the carousel starts")]
    [SerializeField] private float _leftAnchor = 80f;

    // Components
    private VisualElement _carouselContent;
    private Label _titleLabel;
    private List<VisualElement> _cards = new List<VisualElement>();

    // State
    private int _selectedIndex = 0;

    // Input
    private InputAction _navigateAction;
    private InputAction _submitAction;

    private void OnEnable()
    {
        _navigateAction = new InputAction("Navigate", type: InputActionType.Button);
        _navigateAction.AddBinding("<Keyboard>/a");
        _navigateAction.AddBinding("<Keyboard>/leftArrow");
        _navigateAction.AddBinding("<Keyboard>/d");
        _navigateAction.AddBinding("<Keyboard>/rightArrow");

        _submitAction = new InputAction("Submit", type: InputActionType.Button);
        _submitAction.AddBinding("<Keyboard>/space");
        _submitAction.AddBinding("<Keyboard>/enter");

        _navigateAction.performed += OnNavigate;
        _submitAction.performed += OnSubmit;
        _navigateAction.Enable();
        _submitAction.Enable();
    }

    private void OnDisable()
    {
        _navigateAction.Disable();
        _submitAction.Disable();
    }

    private void Awake()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _carouselContent = root.Q<VisualElement>("CarouselContent");
        _titleLabel = root.Q<Label>("GameTitle");

        CreateCards();

        // Wait one frame for UI Layout to calculate
        StartCoroutine(InitialSetup());
    }

    private IEnumerator InitialSetup()
    {
        yield return null;
        UpdateSelection(0);
    }

    private void CreateCards()
    {
        _carouselContent.Clear();
        _cards.Clear();

        for (int i = 0; i < _availableGames.Count; i++)
        {
            var data = _availableGames[i];
            var card = new VisualElement();
            card.AddToClassList("game-card");

            if (data.GameLogo != null)
                card.style.backgroundImage = new StyleBackground(data.GameLogo);

            _carouselContent.Add(card);
            _cards.Add(card);

            int index = i;
            card.RegisterCallback<ClickEvent>(evt => UpdateSelection(index));
        }
    }

    private void OnNavigate(InputAction.CallbackContext ctx)
    {
        string path = ctx.control.path;
        int direction = (path.Contains("left") || path.Contains("/a")) ? -1 : 1;

        int newIndex = Mathf.Clamp(_selectedIndex + direction, 0, _availableGames.Count - 1);
        UpdateSelection(newIndex);
    }

    private void OnSubmit(InputAction.CallbackContext ctx)
    {
        var game = _availableGames[_selectedIndex];
        Debug.Log($"Loading {game.GameName}");
    }

    private void UpdateSelection(int index)
    {
        _selectedIndex = index;

        // 1. Update Visuals
        for (int i = 0; i < _cards.Count; i++)
        {
            if (i == index) _cards[i].AddToClassList("game-card--selected");
            else _cards[i].RemoveFromClassList("game-card--selected");
        }

        // 2. Update Text
        if (index < _availableGames.Count)
            _titleLabel.text = _availableGames[index].GameName;

        // 3. Move Carousel
        // We want the SELECTED card to sit at _leftAnchor.
        // So we shift Left by the sum of all PREVIOUS cards' widths.
        // Since previous cards are unselected, they are _normalWidth.

        float shiftAmount = 0f;
        for (int i = 0; i < index; i++)
        {
            shiftAmount += (_normalWidth + _spacing);
        }

        // Apply the negative shift + the anchor offset
        _carouselContent.style.left = _leftAnchor - shiftAmount;
    }
}