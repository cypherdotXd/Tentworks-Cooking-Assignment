using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages one or more modular ItemPicker components for the Player or any multi-slot character.
/// Coordinates picking, dropping, and smart contextual interactions across all attached pickers.
/// </summary>
[DisallowMultipleComponent]
public class PlayerPickerManager : MonoBehaviour
{
    [Header("Pickers")]
    [Tooltip("List of ItemPicker components managed by this player. If empty, automatically detected in children.")]
    [SerializeField] private List<ItemPicker> pickers = new();

    [Header("Unity Events")]
    [Tooltip("Invoked when any managed picker picks up an item.")]
    public UnityEvent<ItemPicker, PickableItem> onAnyItemPicked = new();

    [Tooltip("Invoked when any managed picker drops an item.")]
    public UnityEvent<ItemPicker, PickableItem> onAnyItemDropped = new();

    [Tooltip("Invoked when triggers on any managed picker detect a change in nearby items or stations.")]
    public UnityEvent onTriggerStateChanged = new();

    public IReadOnlyList<ItemPicker> Pickers => pickers;
    public int TotalPickers => pickers.Count;
    public int HeldItemCount => pickers.Count(p => p != null && p.IsHoldingItem);
    public bool HasFreePicker => pickers.Any(p => p != null && !p.IsHoldingItem);
    public bool HasHeldItem => pickers.Any(p => p != null && p.IsHoldingItem);

    // C# Events
    public event Action<ItemPicker, PickableItem> OnAnyItemPicked;
    public event Action<ItemPicker, PickableItem> OnAnyItemDropped;
    public event Action OnTriggerStateChanged;

    private void Awake()
    {
        InitializePickers();
    }

    private void OnEnable()
    {
        InitializePickers();
        SubscribeToPickers();
    }

    private void OnDisable()
    {
        UnsubscribeFromPickers();
    }

    private void OnTriggerEnter(Collider other)
    {
        for (int i = 0; i < pickers.Count; i++)
        {
            if (pickers[i] != null)
            {
                pickers[i].HandleTriggerEnter(other);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        for (int i = 0; i < pickers.Count; i++)
        {
            if (pickers[i] != null)
            {
                pickers[i].HandleTriggerExit(other);
            }
        }
    }

    /// <summary>
    /// Finds or registers all ItemPicker components on this object or its children.
    /// </summary>
    public void InitializePickers()
    {
        if (pickers == null || pickers.Count == 0)
        {
            pickers = new List<ItemPicker>(GetComponentsInChildren<ItemPicker>(true));
        }

        // If still none found, automatically add a default ItemPicker
        if (pickers.Count == 0)
        {
            ItemPicker defaultPicker = gameObject.AddComponent<ItemPicker>();
            defaultPicker.PickerName = "Default Hand";
            pickers.Add(defaultPicker);
        }
    }

    /// <summary>
    /// Returns the first picker that is currently not holding an item.
    /// </summary>
    public ItemPicker GetFirstFreePicker()
    {
        for (int i = 0; i < pickers.Count; i++)
        {
            if (pickers[i] != null && !pickers[i].IsHoldingItem)
            {
                return pickers[i];
            }
        }
        return null;
    }

    /// <summary>
    /// Returns the first picker that is currently holding an item.
    /// </summary>
    public ItemPicker GetFirstOccupiedPicker()
    {
        for (int i = 0; i < pickers.Count; i++)
        {
            if (pickers[i] != null && pickers[i].IsHoldingItem)
            {
                return pickers[i];
            }
        }
        return null;
    }

    /// <summary>
    /// Finds a specific picker by its assigned name (e.g. 'LeftHand', 'RightHand').
    /// </summary>
    public ItemPicker GetPickerByName(string name)
    {
        for (int i = 0; i < pickers.Count; i++)
        {
            if (pickers[i] != null && string.Equals(pickers[i].PickerName, name, StringComparison.OrdinalIgnoreCase))
            {
                return pickers[i];
            }
        }
        return null;
    }

    /// <summary>
    /// Checks if any managed picker detects an item in reach (loose in trigger or on a nearby drop area).
    /// </summary>
    public bool HasItemInReach()
    {
        for (int i = 0; i < pickers.Count; i++)
        {
            if (pickers[i] != null && pickers[i].HasItemInReach())
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns true if the player has at least one free hand and an item is in reach to pick up.
    /// </summary>
    public bool CanPickAny()
    {
        return HasFreePicker && HasItemInReach();
    }

    /// <summary>
    /// Returns true if the player is currently carrying at least one item
    /// AND is near or touching a drop area that can accept the item.
    /// </summary>
    public bool CanDropAny()
    {
        if (!HasHeldItem) return false;

        for (int i = 0; i < pickers.Count; i++)
        {
            if (pickers[i] != null && pickers[i].CanDrop())
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Picks up the closest available item using the first free picker.
    /// </summary>
    public bool TryPickClosest()
    {
        ItemPicker freePicker = GetFirstFreePicker();
        if (freePicker == null) return false;

        return freePicker.TryPickClosest();
    }

    /// <summary>
    /// Picks up a specific item using the first free picker.
    /// </summary>
    public bool TryPick(PickableItem item)
    {
        ItemPicker freePicker = GetFirstFreePicker();
        if (freePicker == null) return false;

        return freePicker.TryPick(item);
    }

    /// <summary>
    /// Aggregates all pickable items currently in reach of any managed picker (from drop areas or loose).
    /// </summary>
    public List<PickableItem> GetAvailableNearbyItems()
    {
        var allItems = new List<PickableItem>();
        for (int i = 0; i < pickers.Count; i++)
        {
            if (pickers[i] != null)
            {
                var nearby = pickers[i].GetNearbyPickableItems();
                if (nearby != null)
                {
                    for (int j = 0; j < nearby.Count; j++)
                    {
                        if (nearby[j] != null && !allItems.Contains(nearby[j]))
                        {
                            allItems.Add(nearby[j]);
                        }
                    }
                }
            }
        }
        return allItems;
    }

    /// <summary>
    /// Drops the item held by the first occupied picker into the world.
    /// </summary>
    public PickableItem DropFirstHeld(Vector3? customDropPosition = null)
    {
        ItemPicker occupiedPicker = GetFirstOccupiedPicker();
        if (occupiedPicker == null) return null;

        return occupiedPicker.Drop(customDropPosition);
    }

    /// <summary>
    /// Drops an item from the first occupied picker into the specified drop area.
    /// </summary>
    public bool TryDropTo(ItemDropArea dropArea)
    {
        ItemPicker occupiedPicker = GetFirstOccupiedPicker();
        if (occupiedPicker == null || dropArea == null) return false;

        return occupiedPicker.TryDropTo(dropArea);
    }

    /// <summary>
    /// Drops an item from an occupied hand into a nearby drop area.
    /// Drop is only possible when near or touching a drop area that can accept the item.
    /// </summary>
    public bool TryDrop()
    {
        if (!HasHeldItem) return false;

        for (int i = 0; i < pickers.Count; i++)
        {
            ItemPicker picker = pickers[i];
            if (picker != null && picker.IsHoldingItem && picker.CanDrop())
            {
                ItemDropArea nearbyArea = picker.GetClosestDropArea();
                if (nearbyArea != null && picker.TryDropTo(nearbyArea))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Context-sensitive interaction for multi-picker characters:
    /// 1. If near a drop area that has space and player is holding an item: places item into area.
    /// 2. If near a drop area that has items and player has a free picker: takes item from area.
    /// 3. If near a loose pickable item and has a free picker: picks up item.
    /// </summary>
    public bool TryInteract()
    {
        ItemDropArea nearbyArea = GetClosestDropArea();

        // Check if we should place an item into the drop area
        if (nearbyArea != null && HasHeldItem)
        {
            ItemPicker occupiedPicker = GetFirstOccupiedPicker();
            if (occupiedPicker != null && occupiedPicker.CanDrop())
            {
                if (occupiedPicker.TryDropTo(nearbyArea))
                {
                    return true;
                }
            }
        }

        // Check if we should pick up an item from the drop area
        if (nearbyArea != null && !nearbyArea.IsEmpty && HasFreePicker)
        {
            ItemPicker freePicker = GetFirstFreePicker();
            if (freePicker != null)
            {
                PickableItem item = nearbyArea.TryTakeItem();
                if (item != null)
                {
                    if (freePicker.TryPickDirect(item))
                    {
                        return true;
                    }
                    else
                    {
                        nearbyArea.TryAddItem(item);
                    }
                }
            }
        }

        // Check if we can pick up a loose item in the world
        if (HasFreePicker)
        {
            if (TryPickClosest())
            {
                return true;
            }
        }

        return false;
    }

    public ItemDropArea GetClosestDropArea()
    {
        for (int i = 0; i < pickers.Count; i++)
        {
            if (pickers[i] != null)
            {
                ItemDropArea area = pickers[i].GetClosestDropArea();
                if (area != null) return area;
            }
        }
        return null;
    }

    private void SubscribeToPickers()
    {
        for (int i = 0; i < pickers.Count; i++)
        {
            ItemPicker p = pickers[i];
            if (p != null)
            {
                p.OnPicked += HandleItemPicked;
                p.OnDropped += HandleItemDropped;
                p.OnTriggerStateChanged += HandleTriggerStateChanged;
            }
        }
    }

    private void UnsubscribeFromPickers()
    {
        for (int i = 0; i < pickers.Count; i++)
        {
            ItemPicker p = pickers[i];
            if (p != null)
            {
                p.OnPicked -= HandleItemPicked;
                p.OnDropped -= HandleItemDropped;
                p.OnTriggerStateChanged -= HandleTriggerStateChanged;
            }
        }
    }

    private void HandleItemPicked(PickableItem item)
    {
        ItemPicker picker = item != null ? item.CurrentPicker : null;
        OnAnyItemPicked?.Invoke(picker, item);
        onAnyItemPicked?.Invoke(picker, item);
        HandleTriggerStateChanged();
    }

    private void HandleItemDropped(PickableItem item)
    {
        OnAnyItemDropped?.Invoke(null, item);
        onAnyItemDropped?.Invoke(null, item);
        HandleTriggerStateChanged();
    }

    private void HandleTriggerStateChanged()
    {
        OnTriggerStateChanged?.Invoke();
        onTriggerStateChanged?.Invoke();
    }
}
