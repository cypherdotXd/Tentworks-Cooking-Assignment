using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Defines an area or station where items can be placed/dropped (e.g. prep table, stove, cutting board).
/// Uses a trigger Collider to detect picker presence (no distance calculation).
/// The capacity is determined entirely by the number of configured dropSlots.
/// Supports programmatic injection/addition of items without requiring an ItemPicker.
/// </summary>
[DisallowMultipleComponent]
public class ItemDropArea : MonoBehaviour
{
    private static readonly List<ItemDropArea> s_ActiveDropAreas = new();
    public static IReadOnlyList<ItemDropArea> ActiveDropAreas => s_ActiveDropAreas;

    [Header("Slots")]
    [Tooltip("Transforms marking the physical slot positions for dropped items. Total capacity equals the number of slots.")]
    [SerializeField] private List<Transform> dropSlots = new();

    [Header("Starting Items")]
    [Tooltip("Optional list of PickableItem scene objects or prefabs to place in slots at the start of the game.")]
    [SerializeField] private List<PickableItem> initialItems = new();


    [Header("Drop Settings")]
    [Tooltip("Whether players and external systems are allowed to drop/place items into this area. If false, dropping is disabled.")]
    [SerializeField] private bool allowDrop = true;

    [Header("Unity Events")]
    [Tooltip("Invoked when an item is placed in this drop area.")]
    public UnityEvent<PickableItem> onItemAdded = new();

    [Tooltip("Invoked when an item is removed from this drop area.")]
    public UnityEvent<PickableItem> onItemRemoved = new();

    // Internal item storage & tracked pickers
    protected readonly List<PickableItem> currentItems = new();
    private readonly List<ItemPicker> pickersInTrigger = new();

    public bool AllowDrop
    {
        get => allowDrop;
        set
        {
            if (allowDrop != value)
            {
                allowDrop = value;
                NotifyPickersInTrigger();
            }
        }
    }

    public List<Transform> DropSlots => dropSlots;
    public List<PickableItem> InitialItems => initialItems;
    public int MaxCapacity => dropSlots != null ? dropSlots.Count : 0;
    public int SlotCount => MaxCapacity;

    public IReadOnlyList<PickableItem> CurrentItems => currentItems;
    public IReadOnlyList<ItemPicker> PickersInTrigger => pickersInTrigger;
    public int ItemCount => currentItems.Count;
    public bool HasSpace => allowDrop && currentItems.Count < MaxCapacity;
    public bool IsEmpty => currentItems.Count == 0;

    // C# Events
    public event Action<PickableItem> OnItemAdded;
    public event Action<PickableItem> OnItemRemoved;

    private void Start()
    {
        InitializeStartingItems();
    }

    private void OnEnable()
    {
        if (!s_ActiveDropAreas.Contains(this))
        {
            s_ActiveDropAreas.Add(this);
        }
    }

    private void OnDisable()
    {
        s_ActiveDropAreas.Remove(this);
    }

    /// <summary>
    /// Pre-populates the slots with starting items if configured.
    /// Supports both scene objects, prefabs, and items already childed to slots.
    /// </summary>
    private void InitializeStartingItems()
    {
        // 1. Process items explicitly assigned in initialItems
        if (initialItems != null)
        {
            for (int i = 0; i < initialItems.Count; i++)
            {
                PickableItem item = initialItems[i];
                if (item == null || !HasSpace) continue;

                InjectItem(item);
            }
        }

        // 2. Auto-discover any PickableItems already placed under slot transforms in the editor
        if (dropSlots != null)
        {
            for (int i = 0; i < dropSlots.Count; i++)
            {
                Transform slot = dropSlots[i];
                if (slot == null || !HasSpace) continue;

                var childItems = slot.GetComponentsInChildren<PickableItem>(true);
                for (int j = 0; j < childItems.Length; j++)
                {
                    PickableItem childItem = childItems[j];
                    if (childItem != null && !currentItems.Contains(childItem) && HasSpace)
                    {
                        currentItems.Add(childItem);
                        childItem.OnPlacedInDropArea(this, slot);
                    }
                }
            }
        }

        NotifyPickersInTrigger();
    }

    private void OnTriggerEnter(Collider other)
    {
        ItemPicker picker = other.GetComponentInParent<ItemPicker>();
        if (picker != null)
        {
            if (!pickersInTrigger.Contains(picker))
            {
                pickersInTrigger.Add(picker);
            }
            picker.NotifyTriggerStateChanged();
        }
        else
        {
            PlayerPickerManager manager = other.GetComponentInParent<PlayerPickerManager>();
            if (manager != null)
            {
                foreach (var p in manager.Pickers)
                {
                    if (p != null)
                    {
                        if (!pickersInTrigger.Contains(p))
                        {
                            pickersInTrigger.Add(p);
                        }
                        p.NotifyTriggerStateChanged();
                    }
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        ItemPicker picker = other.GetComponentInParent<ItemPicker>();
        if (picker != null)
        {
            pickersInTrigger.Remove(picker);
            picker.NotifyTriggerStateChanged();
        }
        else
        {
            PlayerPickerManager manager = other.GetComponentInParent<PlayerPickerManager>();
            if (manager != null)
            {
                foreach (var p in manager.Pickers)
                {
                    if (p != null)
                    {
                        pickersInTrigger.Remove(p);
                        p.NotifyTriggerStateChanged();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if a picker is currently inside this drop area's trigger volume.
    /// </summary>
    public bool IsPickerInTrigger(ItemPicker picker)
    {
        CleanUpPickerList();
        return picker != null && pickersInTrigger.Contains(picker);
    }

    /// <summary>
    /// Checks if any picker is currently inside this drop area's trigger volume.
    /// </summary>
    public bool HasPickerInTrigger
    {
        get
        {
            CleanUpPickerList();
            return pickersInTrigger.Count > 0;
        }
    }

    public Func<PickableItem, bool> ItemFilter { get; set; }

    /// <summary>
    /// Checks if a given item can be accepted by this drop area.
    /// Returns true as long as the item is non-null and the drop area has available space.
    /// </summary>
    public virtual bool CanAcceptItem(PickableItem item)
    {
        if (!allowDrop || item == null) return false;
        if (ItemFilter != null && !ItemFilter(item)) return false;
        return HasSpace;
    }

    /// <summary>
    /// Places an item into the next available slot in this drop area.
    /// Can be called directly from code without requiring an ItemPicker.
    /// </summary>
    public virtual bool TryAddItem(PickableItem item)
    {
        if (!CanAcceptItem(item)) return false;

        Transform slot = GetNextAvailableSlot();
        if (slot == null) return false;

        currentItems.Add(item);
        item.OnPlacedInDropArea(this, slot);

        OnItemAdded?.Invoke(item);
        onItemAdded?.Invoke(item);
        NotifyPickersInTrigger();
        return true;
    }

    /// <summary>
    /// Directly registers an item into this drop area and slot without checking allowDrop.
    /// Intended for external dispensers, fridges, or procedural spawner systems.
    /// </summary>
    public virtual void RegisterPlacedItem(PickableItem item, Transform slotTransform)
    {
        if (item == null) return;

        if (!currentItems.Contains(item))
        {
            currentItems.Add(item);
        }
        item.OnPlacedInDropArea(this, slotTransform);

        OnItemAdded?.Invoke(item);
        onItemAdded?.Invoke(item);
        NotifyPickersInTrigger();
    }

    /// <summary>
    /// Injects an item (either an existing scene instance or a project prefab) directly into the next available slot.
    /// Does not require an ItemPicker or player interaction.
    /// Returns the placed PickableItem instance, or null if full or incompatible.
    /// </summary>
    public virtual PickableItem InjectItem(PickableItem itemOrPrefab)
    {
        if (itemOrPrefab == null || !HasSpace) return null;

        PickableItem instance = itemOrPrefab;
        bool isPrefab = !itemOrPrefab.gameObject.scene.IsValid();

        if (isPrefab)
        {
            instance = Instantiate(itemOrPrefab);
        }

        if (TryAddItem(instance))
        {
            return instance;
        }

        // If placement failed and we instantiated a new prefab instance, clean it up
        if (isPrefab && instance != null)
        {
            Destroy(instance.gameObject);
        }

        return null;
    }

    /// <summary>
    /// Injects a GameObject prefab or scene object directly into the next available slot.
    /// </summary>
    public virtual PickableItem InjectItem(GameObject gameObjectOrPrefab)
    {
        if (gameObjectOrPrefab == null) return null;

        PickableItem item = gameObjectOrPrefab.GetComponent<PickableItem>();
        if (item == null)
        {
            Debug.LogWarning($"[ItemDropArea] Cannot inject {gameObjectOrPrefab.name}: missing PickableItem component.", this);
            return null;
        }

        return InjectItem(item);
    }

    /// <summary>
    /// Injects an item directly into a specific slot index (0 to dropSlots.Count - 1).
    /// Returns the placed PickableItem, or null if the slot is invalid, occupied, or incompatible.
    /// </summary>
    public virtual PickableItem InjectItemAtSlot(PickableItem itemOrPrefab, int slotIndex)
    {
        if (itemOrPrefab == null) return null;
        if (dropSlots == null || slotIndex < 0 || slotIndex >= dropSlots.Count) return null;

        Transform slot = dropSlots[slotIndex];
        if (slot == null || IsSlotOccupied(slot)) return null;

        if (!CanAcceptItem(itemOrPrefab)) return null;

        PickableItem instance = itemOrPrefab;
        bool isPrefab = !itemOrPrefab.gameObject.scene.IsValid();

        if (isPrefab)
        {
            instance = Instantiate(itemOrPrefab);
        }

        currentItems.Add(instance);
        instance.OnPlacedInDropArea(this, slot);

        OnItemAdded?.Invoke(instance);
        onItemAdded?.Invoke(instance);
        NotifyPickersInTrigger();

        return instance;
    }

    /// <summary>
    /// Checks whether a specific slot Transform currently has an item placed on it.
    /// </summary>
    public bool IsSlotOccupied(Transform slot)
    {
        if (slot == null) return false;

        for (int i = 0; i < currentItems.Count; i++)
        {
            if (currentItems[i] != null && currentItems[i].transform.parent == slot)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether this drop area has at least one item that is currently pickable.
    /// </summary>
    public bool HasPickableItem()
    {
        for (int i = 0; i < currentItems.Count; i++)
        {
            if (currentItems[i] != null && currentItems[i].IsPickable)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns the distinct pickable items currently available in this drop area.
    /// For multi-item slots (e.g. stacks in a fridge), returns the top pickable item for each slot.
    /// </summary>
    public virtual List<PickableItem> GetAvailablePickableItems()
    {
        var items = new List<PickableItem>();
        if (dropSlots != null && dropSlots.Count > 0)
        {
            for (int s = 0; s < dropSlots.Count; s++)
            {
                Transform slot = dropSlots[s];
                if (slot == null) continue;

                PickableItem topItem = null;
                for (int i = currentItems.Count - 1; i >= 0; i--)
                {
                    PickableItem item = currentItems[i];
                    if (item != null && item.IsPickable && (item.transform.parent == slot || item.transform.IsChildOf(slot)))
                    {
                        topItem = item;
                        break;
                    }
                }
                if (topItem != null && !items.Contains(topItem))
                {
                    items.Add(topItem);
                }
            }
        }
        else
        {
            for (int i = 0; i < currentItems.Count; i++)
            {
                PickableItem item = currentItems[i];
                if (item != null && item.IsPickable && !items.Contains(item))
                {
                    items.Add(item);
                }
            }
        }
        return items;
    }

    /// <summary>
    /// Removes and returns the first available pickable item from this drop area.
    /// </summary>
    public virtual PickableItem TryTakeItem()
    {
        if (currentItems.Count == 0) return null;

        for (int i = currentItems.Count - 1; i >= 0; i--)
        {
            PickableItem item = currentItems[i];
            if (item != null && item.enabled)
            {
                currentItems.RemoveAt(i);
                OnItemRemoved?.Invoke(item);
                onItemRemoved?.Invoke(item);
                NotifyPickersInTrigger();
                return item;
            }
        }

        return null;
    }

    /// <summary>
    /// Removes a specific item from this drop area (e.g. when picked up directly by a player).
    /// </summary>
    public virtual bool RemoveItem(PickableItem item)
    {
        if (item == null) return false;

        if (currentItems.Remove(item))
        {
            OnItemRemoved?.Invoke(item);
            onItemRemoved?.Invoke(item);
            NotifyPickersInTrigger();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns the first empty slot Transform, or null if all defined slots are occupied.
    /// </summary>
    public Transform GetNextAvailableSlot()
    {
        if (dropSlots == null) return null;

        for (int i = 0; i < dropSlots.Count; i++)
        {
            Transform slot = dropSlots[i];
            if (slot != null && !IsSlotOccupied(slot))
            {
                return slot;
            }
        }

        return null;
    }

    public void NotifyPickersInTrigger()
    {
        CleanUpPickerList();
        for (int i = 0; i < pickersInTrigger.Count; i++)
        {
            if (pickersInTrigger[i] != null)
            {
                pickersInTrigger[i].NotifyTriggerStateChanged();
            }
        }
    }

    private void CleanUpPickerList()
    {
        for (int i = pickersInTrigger.Count - 1; i >= 0; i--)
        {
            if (pickersInTrigger[i] == null || !pickersInTrigger[i].gameObject.activeInHierarchy)
            {
                pickersInTrigger.RemoveAt(i);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (dropSlots != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var slot in dropSlots)
            {
                if (slot != null)
                {
                    Gizmos.DrawWireCube(slot.position, Vector3.one * 0.25f);
                }
            }
        }
    }
}
