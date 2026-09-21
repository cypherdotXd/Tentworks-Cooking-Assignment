using UnityEngine;

/// <summary>
/// Presenter component in the MVP architecture.
/// Mediates between the gameplay layer (PlayerPickerManager) and the UI layer (PlayerInteractionView).
/// Reacts to trigger enter/exit and item pickup/drop events to toggle buttons and update the held item tooltip.
/// </summary>
[DisallowMultipleComponent]
public class PlayerInteractionUIPresenter : MonoBehaviour
{
    [Header("Model & View")]
    [Tooltip("Reference to the player's picker manager. If null, automatically discovered in scene.")]
    [SerializeField] private PlayerPickerManager pickerManager;

    [Tooltip("Reference to the interaction UI view. If null, automatically discovered on this GameObject.")]
    [SerializeField] private PlayerInteractionView view;

    [Header("Tooltip Configuration")]
    [Tooltip("String format used to display the currently carried item.")]
    [SerializeField] private string tooltipFormat = "In hand: {0}";

    [Tooltip("Default casual text displayed in the tooltip when empty-handed.")]
    [SerializeField] private string defaultTooltipText = "Hands empty";

    [Tooltip("Whether the tooltip should be completely hidden when the player is not holding any item.")]
    [SerializeField] private bool hideTooltipWhenEmpty = false;

    public PlayerPickerManager PickerManager => pickerManager;
    public PlayerInteractionView View => view;

    private void Awake()
    {
        EnsureReferences();
    }

    private void Start()
    {
        EnsureReferences();
        RefreshUI();
    }

    private void OnEnable()
    {
        EnsureReferences();
        SubscribeEvents();
        RefreshUI();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private int lastItemCount = -1;

    private void Update()
    {
        if (pickerManager == null || view == null) return;

        bool isGameInactiveOrPaused = GameManager.Instance != null && (!GameManager.Instance.IsGameActive || GameManager.Instance.IsPaused);
        bool canPick = !isGameInactiveOrPaused && pickerManager.CanPickAny();
        bool canDrop = !isGameInactiveOrPaused && pickerManager.CanDropAny();
        var nearby = pickerManager.GetAvailableNearbyItems();
        int currentCount = nearby != null ? nearby.Count : 0;

        if ((view.PickButton != null && view.PickButton.interactable != canPick) ||
            (view.DropButton != null && view.DropButton.interactable != canDrop) ||
            lastItemCount != currentCount)
        {
            lastItemCount = currentCount;
            RefreshUI();
        }
    }

    /// <summary>
    /// Evaluates current gameplay state and pushes updates to the View.
    /// Called automatically whenever picker or trigger events fire.
    /// </summary>
    public void RefreshUI()
    {
        if (view == null || pickerManager == null) return;

        // 1. Tooltip logic: show held item name
        bool isHolding = pickerManager.HasHeldItem;
        if (isHolding)
        {
            ItemPicker occupied = pickerManager.GetFirstOccupiedPicker();
            string itemName = occupied?.HeldItem != null ? occupied.HeldItem.ItemName : "Item";
            view.SetTooltip(string.Format(tooltipFormat, itemName), true);
        }
        else
        {
            view.SetTooltip(defaultTooltipText, !hideTooltipWhenEmpty);
        }

        // 2. Pick buttons logic: find available items in reach and configure dynamic pick buttons
        bool isGameInactiveOrPaused = GameManager.Instance != null && (!GameManager.Instance.IsGameActive || GameManager.Instance.IsPaused);
        bool canPick = !isGameInactiveOrPaused && pickerManager.CanPickAny();
        var availableItems = pickerManager.GetAvailableNearbyItems();
        view.SetPickButtons(availableItems, canPick);

        // 3. Drop button logic: enabled only if player is holding an item AND near an allowed drop area
        bool canDrop = !isGameInactiveOrPaused && pickerManager.CanDropAny();
        view.SetDropButtonInteractable(canDrop);
    }

    private void HandlePickClicked()
    {
        if (GameManager.Instance != null && (!GameManager.Instance.IsGameActive || GameManager.Instance.IsPaused)) return;
        if (pickerManager != null && pickerManager.CanPickAny())
        {
            pickerManager.TryPickClosest();
        }
    }

    private void HandlePickSpecificItem(PickableItem item)
    {
        if (GameManager.Instance != null && (!GameManager.Instance.IsGameActive || GameManager.Instance.IsPaused)) return;
        if (pickerManager != null && pickerManager.CanPickAny() && item != null)
        {
            pickerManager.TryPick(item);
        }
    }

    private void HandleDropClicked()
    {
        if (GameManager.Instance != null && (!GameManager.Instance.IsGameActive || GameManager.Instance.IsPaused)) return;
        if (pickerManager != null && pickerManager.CanDropAny())
        {
            pickerManager.TryDrop();
        }
    }

    private void EnsureReferences()
    {
        if (view == null)
        {
            view = GetComponent<PlayerInteractionView>();
            if (view == null)
            {
                view = GetComponentInChildren<PlayerInteractionView>(true);
            }
        }

        if (pickerManager == null)
        {
            pickerManager = GetComponentInParent<PlayerPickerManager>();
            if (pickerManager == null)
            {
                pickerManager = FindAnyObjectByType<PlayerPickerManager>();
            }
        }
    }

    private void SubscribeEvents()
    {
        if (view != null)
        {
            view.OnPickClicked += HandlePickClicked;
            view.OnPickSpecificItem += HandlePickSpecificItem;
            view.OnDropClicked += HandleDropClicked;
        }

        if (pickerManager != null)
        {
            pickerManager.OnAnyItemPicked += HandleItemStateChanged;
            pickerManager.OnAnyItemDropped += HandleItemStateChanged;
            pickerManager.OnTriggerStateChanged += HandleTriggerStateChanged;
        }

        // Also subscribe to all active drop areas so if items are placed/taken by external systems, UI reacts
        var dropAreas = ItemDropArea.ActiveDropAreas;
        for (int i = 0; i < dropAreas.Count; i++)
        {
            if (dropAreas[i] != null)
            {
                dropAreas[i].OnItemAdded += HandleDropAreaItemChanged;
                dropAreas[i].OnItemRemoved += HandleDropAreaItemChanged;
            }
        }
    }

    private void UnsubscribeEvents()
    {
        if (view != null)
        {
            view.OnPickClicked -= HandlePickClicked;
            view.OnPickSpecificItem -= HandlePickSpecificItem;
            view.OnDropClicked -= HandleDropClicked;
        }

        if (pickerManager != null)
        {
            pickerManager.OnAnyItemPicked -= HandleItemStateChanged;
            pickerManager.OnAnyItemDropped -= HandleItemStateChanged;
            pickerManager.OnTriggerStateChanged -= HandleTriggerStateChanged;
        }

        var dropAreas = ItemDropArea.ActiveDropAreas;
        for (int i = 0; i < dropAreas.Count; i++)
        {
            if (dropAreas[i] != null)
            {
                dropAreas[i].OnItemAdded -= HandleDropAreaItemChanged;
                dropAreas[i].OnItemRemoved -= HandleDropAreaItemChanged;
            }
        }
    }

    private void HandleItemStateChanged(ItemPicker picker, PickableItem item)
    {
        RefreshUI();
    }

    private void HandleTriggerStateChanged()
    {
        RefreshUI();
    }

    private void HandleDropAreaItemChanged(PickableItem item)
    {
        RefreshUI();
    }
}
