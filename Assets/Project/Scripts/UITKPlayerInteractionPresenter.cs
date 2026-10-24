using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public class UITKPlayerInteractionPresenter : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private PlayerPickerManager _pickerManager;
    [SerializeField] private string _tooltipFormat = "In hand: {0}";
    [SerializeField] private string _defaultTooltipText = "Hands empty";
    [SerializeField] private bool _hideTooltipWhenEmpty = false;

    private VisualElement _container;
    private VisualElement _interactionTooltip;
    private Label _tooltipLabel;
    private Button _pickButton;
    private Button _dropButton;
    private VisualElement _actionButtonsRow;

    private readonly List<Button> _spawnedPickButtons = new();
    private PickableItem _defaultButtonTargetItem;

    private bool _lastCanPick;
    private bool _lastCanDrop;
    private int _lastNearbyCount = -1;

    void OnEnable()
    {
        VisualElement root = _uiDocument.rootVisualElement;
        if (root == null) return;

        _container = root.Q<VisualElement>("playerInteractionContainer");
        _interactionTooltip = root.Q<VisualElement>("interactionTooltip");
        _tooltipLabel = root.Q<Label>("tooltipText");
        _pickButton = root.Q<Button>("pickButton");
        _dropButton = root.Q<Button>("dropButton");
        _actionButtonsRow = root.Q<VisualElement>("actionButtonsRow");

        if (_pickButton != null) _pickButton.clicked += HandlePickClicked;
        if (_dropButton != null) _dropButton.clicked += HandleDropClicked;

        if (_pickerManager != null)
        {
            _pickerManager.OnAnyItemPicked += HandleItemStateChanged;
            _pickerManager.OnAnyItemDropped += HandleItemStateChanged;
            _pickerManager.OnTriggerStateChanged += HandleTriggerStateChanged;
        }

        var dropAreas = ItemDropArea.ActiveDropAreas;
        for (int i = 0; i < dropAreas.Count; i++)
        {
            if (dropAreas[i] != null)
            {
                dropAreas[i].OnItemAdded += HandleDropAreaChanged;
                dropAreas[i].OnItemRemoved += HandleDropAreaChanged;
            }
        }

        _lastCanPick = false;
        _lastCanDrop = false;
        _lastNearbyCount = -1;
        RefreshUI();
    }

    void OnDisable()
    {
        if (_pickButton != null) _pickButton.clicked -= HandlePickClicked;
        if (_dropButton != null) _dropButton.clicked -= HandleDropClicked;

        if (_pickerManager != null)
        {
            _pickerManager.OnAnyItemPicked -= HandleItemStateChanged;
            _pickerManager.OnAnyItemDropped -= HandleItemStateChanged;
            _pickerManager.OnTriggerStateChanged -= HandleTriggerStateChanged;
        }

        var dropAreas = ItemDropArea.ActiveDropAreas;
        for (int i = 0; i < dropAreas.Count; i++)
        {
            if (dropAreas[i] != null)
            {
                dropAreas[i].OnItemAdded -= HandleDropAreaChanged;
                dropAreas[i].OnItemRemoved -= HandleDropAreaChanged;
            }
        }
    }

    void Update()
    {
        if (_pickerManager == null || _tooltipLabel == null) return;

        bool isGameInactive = GameManager.Instance != null && (!GameManager.Instance.IsGameActive || GameManager.Instance.IsPaused);
        bool canPick = !isGameInactive && _pickerManager.CanPickAny();
        bool canDrop = !isGameInactive && _pickerManager.CanDropAny();
        var nearby = _pickerManager.GetAvailableNearbyItems();
        int nearbyCount = nearby != null ? nearby.Count : 0;

        if (canPick != _lastCanPick || canDrop != _lastCanDrop || nearbyCount != _lastNearbyCount)
        {
            _lastCanPick = canPick;
            _lastCanDrop = canDrop;
            _lastNearbyCount = nearbyCount;
            RefreshUI();
        }
    }

    public void RefreshUI()
    {
        if (_pickerManager == null || _tooltipLabel == null) return;

        bool isGameInactive = GameManager.Instance != null && (!GameManager.Instance.IsGameActive || GameManager.Instance.IsPaused);

        // Tooltip
        bool isHolding = _pickerManager.HasHeldItem;
        if (isHolding)
        {
            ItemPicker occupied = _pickerManager.GetFirstOccupiedPicker();
            string itemName = occupied?.HeldItem != null ? occupied.HeldItem.ItemName : "Item";
            _tooltipLabel.text = string.Format(_tooltipFormat, itemName);
            SetTooltipVisible(true);
        }
        else
        {
            _tooltipLabel.text = _defaultTooltipText;
            SetTooltipVisible(!_hideTooltipWhenEmpty);
        }

        // Pick buttons
        bool canPick = !isGameInactive && _pickerManager.CanPickAny();
        var nearbyItems = _pickerManager.GetAvailableNearbyItems();
        SetPickButtons(nearbyItems, canPick);

        // Drop button
        if (_dropButton != null)
        {
            bool canDrop = !isGameInactive && _pickerManager.CanDropAny();
            _dropButton.SetEnabled(canDrop);
        }
    }

    private void SetTooltipVisible(bool visible)
    {
        if (_interactionTooltip == null) return;
        if (visible)
            _interactionTooltip.RemoveFromClassList("hidden");
        else
            _interactionTooltip.AddToClassList("hidden");
    }

    private void SetPickButtons(IReadOnlyList<PickableItem> items, bool canPick)
    {
        if (_pickButton == null || _actionButtonsRow == null) return;

        if (items == null || items.Count <= 1)
        {
            _defaultButtonTargetItem = (items != null && items.Count == 1) ? items[0] : null;
            _pickButton.text = _defaultButtonTargetItem != null ? $"Pick {_defaultButtonTargetItem.ItemName}" : "Pick";
            _pickButton.SetEnabled(canPick && _defaultButtonTargetItem != null);
            _pickButton.RemoveFromClassList("hidden");

            for (int i = 0; i < _spawnedPickButtons.Count; i++)
                _spawnedPickButtons[i].AddToClassList("hidden");
        }
        else
        {
            // Primary button → first item
            _defaultButtonTargetItem = items[0];
            _pickButton.text = $"Pick {items[0].ItemName}";
            _pickButton.SetEnabled(canPick);
            _pickButton.RemoveFromClassList("hidden");

            // Ensure enough extra buttons
            int needed = items.Count - 1;
            while (_spawnedPickButtons.Count < needed)
            {
                var btn = new Button();
                btn.AddToClassList("action-btn");
                btn.AddToClassList("pick-btn");
                int capturedIndex = _spawnedPickButtons.Count + 1;
                btn.clicked += () => HandlePickSpecificButtonClicked(capturedIndex);
                _actionButtonsRow.Add(btn);
                _spawnedPickButtons.Add(btn);
            }

            for (int i = 0; i < _spawnedPickButtons.Count; i++)
            {
                if (i < needed)
                {
                    var item = items[i + 1];
                    var btn = _spawnedPickButtons[i];
                    btn.text = $"Pick {item.ItemName}";
                    btn.SetEnabled(canPick);
                    btn.RemoveFromClassList("hidden");
                }
                else
                {
                    _spawnedPickButtons[i].AddToClassList("hidden");
                }
            }
        }
    }

    private void HandlePickClicked()
    {
        if (GameManager.Instance != null && (!GameManager.Instance.IsGameActive || GameManager.Instance.IsPaused)) return;
        if (_pickerManager == null) return;

        if (_defaultButtonTargetItem != null)
            _pickerManager.TryPick(_defaultButtonTargetItem);
        else if (_pickerManager.CanPickAny())
            _pickerManager.TryPickClosest();
    }

    private void HandlePickSpecificButtonClicked(int spawnedIndex)
    {
        if (GameManager.Instance != null && (!GameManager.Instance.IsGameActive || GameManager.Instance.IsPaused)) return;
        if (_pickerManager == null) return;

        var nearby = _pickerManager.GetAvailableNearbyItems();
        int itemIndex = spawnedIndex; // spawnedIndex 1 → nearby[1], etc.
        if (nearby != null && itemIndex < nearby.Count && nearby[itemIndex] != null)
            _pickerManager.TryPick(nearby[itemIndex]);
    }

    private void HandleDropClicked()
    {
        if (GameManager.Instance != null && (!GameManager.Instance.IsGameActive || GameManager.Instance.IsPaused)) return;
        if (_pickerManager != null && _pickerManager.CanDropAny())
            _pickerManager.TryDrop();
    }

    private void HandleItemStateChanged(ItemPicker picker, PickableItem item)
    {
        RefreshUI();
    }

    private void HandleTriggerStateChanged()
    {
        RefreshUI();
    }

    private void HandleDropAreaChanged(PickableItem item)
    {
        RefreshUI();
    }
}
