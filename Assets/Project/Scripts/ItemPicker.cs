using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Modular component that allows picking up, carrying, and dropping a single PickableItem.
/// Uses a trigger Collider to detect pickable items in range (no distance calculation required).
/// Multiple ItemPicker components can exist on a single character (e.g. Left Hand, Right Hand).
/// </summary>
[DisallowMultipleComponent]
public class ItemPicker : MonoBehaviour
{
    [Header("Picker Identity")]
    [Tooltip("Identifier for this picker instance (e.g. LeftHand, RightHand, Primary).")]
    [SerializeField] private string pickerName = "Primary Hand";

    [Header("Hold Settings")]
    [Tooltip("Transform where the carried item will be attached. If null, this transform is used.")]
    [SerializeField] private Transform holdPoint;

    [Tooltip("Forward drop offset when dropping an item into the world.")]
    [SerializeField] private float dropForwardOffset = 0.8f;

    [Header("Unity Events")]
    [Tooltip("Invoked when an item is picked up by this picker.")]
    public UnityEvent<PickableItem> onItemPicked = new();

    [Tooltip("Invoked when an item is dropped by this picker.")]
    public UnityEvent<PickableItem> onItemDropped = new();

    [Tooltip("Invoked when items or drop areas enter or exit this picker's trigger volume.")]
    public UnityEvent onTriggerStateChanged = new();

    // Internal tracked objects inside the trigger volume
    private readonly List<PickableItem> itemsInTrigger = new();
    private readonly List<ItemDropArea> dropAreasInTrigger = new();

    public string PickerName
    {
        get => pickerName;
        set => pickerName = value;
    }

    public Transform HoldPoint
    {
        get => holdPoint != null ? holdPoint : transform;
        set => holdPoint = value;
    }

    public PickableItem HeldItem { get; private set; }
    public bool IsHoldingItem => HeldItem != null;
    public IReadOnlyList<PickableItem> ItemsInTrigger => itemsInTrigger;
    public IReadOnlyList<ItemDropArea> DropAreasInTrigger => dropAreasInTrigger;

    // C# Events
    public event Action<PickableItem> OnPicked;
    public event Action<PickableItem> OnDropped;
    public event Action OnTriggerStateChanged;

    private void Awake()
    {
        if (holdPoint == null)
        {
            holdPoint = transform;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleTriggerEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        HandleTriggerExit(other);
    }

    /// <summary>
    /// Processes a collider entering this picker's detection volume.
    /// Can be called directly by parent managers or child colliders.
    /// </summary>
    public void HandleTriggerEnter(Collider other)
    {
        bool changed = false;

        PickableItem item = other.GetComponentInParent<PickableItem>();
        if (item != null && !item.IsPickedUp && !itemsInTrigger.Contains(item))
        {
            itemsInTrigger.Add(item);
            changed = true;
        }

        ItemDropArea dropArea = other.GetComponentInParent<ItemDropArea>();
        if (dropArea != null && !dropAreasInTrigger.Contains(dropArea))
        {
            dropAreasInTrigger.Add(dropArea);
            changed = true;
        }

        if (changed)
        {
            NotifyTriggerStateChanged();
        }
    }

    /// <summary>
    /// Processes a collider exiting this picker's detection volume.
    /// Can be called directly by parent managers or child colliders.
    /// </summary>
    public void HandleTriggerExit(Collider other)
    {
        if (other == null) return;
        bool changed = false;

        PickableItem item = other.GetComponentInParent<PickableItem>();
        if (item != null && itemsInTrigger.Remove(item))
        {
            changed = true;
        }

        ItemDropArea dropArea = other.GetComponentInParent<ItemDropArea>();
        if (dropArea != null && dropAreasInTrigger.Remove(dropArea))
        {
            changed = true;
        }

        if (changed)
        {
            NotifyTriggerStateChanged();
        }
    }

    public virtual bool CanPick(PickableItem item)
    {
        if (IsHoldingItem) return false;
        if (item == null || item.IsPickedUp || !item.IsPickable) return false;

        CleanUpTriggerLists();
        if (itemsInTrigger.Contains(item)) return true;

        // Also allow picking if item is resting on a drop area within reach
        if (item.CurrentDropArea != null)
        {
            if (dropAreasInTrigger.Contains(item.CurrentDropArea) || item.CurrentDropArea.IsPickerInTrigger(this))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if there is any pickable item in reach (loose in trigger or on a nearby drop area).
    /// </summary>
    public virtual bool HasItemInReach()
    {
        CleanUpTriggerLists();

        // 1. Check for loose items in trigger
        for (int i = 0; i < itemsInTrigger.Count; i++)
        {
            if (itemsInTrigger[i] != null && !itemsInTrigger[i].IsPickedUp && itemsInTrigger[i].enabled)
            {
                return true;
            }
        }

        // 2. Check for items sitting on drop areas in trigger
        for (int i = 0; i < dropAreasInTrigger.Count; i++)
        {
            if (dropAreasInTrigger[i] != null && dropAreasInTrigger[i].HasPickableItem())
            {
                return true;
            }
        }

        // 3. Check for any drop area whose trigger contains this picker
        var allAreas = ItemDropArea.ActiveDropAreas;
        for (int i = 0; i < allAreas.Count; i++)
        {
            if (allAreas[i] != null && allAreas[i].IsPickerInTrigger(this) && allAreas[i].HasPickableItem())
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to pick up the specified item.
    /// </summary>
    public virtual bool TryPick(PickableItem item)
    {
        if (!CanPick(item)) return false;
        return TryPickDirect(item);
    }

    /// <summary>
    /// Directly attaches and picks up an item (bypassing the itemsInTrigger check, e.g. when taken from a drop area).
    /// </summary>
    public virtual bool TryPickDirect(PickableItem item)
    {
        if (IsHoldingItem || item == null || item.IsPickedUp || !item.enabled) return false;

        HeldItem = item;
        itemsInTrigger.Remove(item);
        item.OnPickedUp(this, HoldPoint);

        OnPicked?.Invoke(item);
        onItemPicked?.Invoke(item);
        NotifyTriggerStateChanged();
        return true;
    }

    /// <summary>
    /// Finds the closest available PickableItem inside the trigger volume.
    /// </summary>
    public virtual PickableItem GetClosestPickableItem()
    {
        CleanUpTriggerLists();
        if (itemsInTrigger.Count == 0) return null;

        PickableItem closest = null;
        float minDistanceSqr = float.MaxValue;
        Vector3 myPos = transform.position;

        for (int i = 0; i < itemsInTrigger.Count; i++)
        {
            PickableItem item = itemsInTrigger[i];
            if (item == null || item.IsPickedUp) continue;

            float distSqr = (item.transform.position - myPos).sqrMagnitude;
            if (distSqr < minDistanceSqr)
            {
                minDistanceSqr = distSqr;
                closest = item;
            }
        }

        return closest;
    }

    /// <summary>
    /// Returns the pickable items currently available nearby (from the closest drop area, or loose floor items).
    /// </summary>
    public virtual List<PickableItem> GetNearbyPickableItems()
    {
        CleanUpTriggerLists();
        ItemDropArea dropArea = GetClosestDropArea();
        if (dropArea != null && !dropArea.IsEmpty)
        {
            var areaItems = dropArea.GetAvailablePickableItems();
            if (areaItems != null && areaItems.Count > 0)
            {
                return areaItems;
            }
        }

        var looseItems = new List<PickableItem>();
        for (int i = 0; i < itemsInTrigger.Count; i++)
        {
            PickableItem item = itemsInTrigger[i];
            if (item != null && item.IsPickable && !item.IsPickedUp && !looseItems.Contains(item))
            {
                looseItems.Add(item);
            }
        }
        return looseItems;
    }

    /// <summary>
    /// Attempts to pick up the closest available item (checking nearby drop areas first, then loose items).
    /// </summary>
    public virtual bool TryPickClosest()
    {
        if (IsHoldingItem) return false;

        // Check if there is a nearby drop area with items first
        ItemDropArea dropArea = GetClosestDropArea();
        if (dropArea != null && !dropArea.IsEmpty)
        {
            PickableItem itemFromArea = dropArea.TryTakeItem();
            if (itemFromArea != null)
            {
                return TryPickDirect(itemFromArea);
            }
        }

        PickableItem closest = GetClosestPickableItem();
        if (closest != null)
        {
            return TryPick(closest);
        }

        return false;
    }

    /// <summary>
    /// Drops the currently held item freely into the world in front of the picker.
    /// </summary>
    public virtual PickableItem Drop(Vector3? customDropPosition = null)
    {
        if (!IsHoldingItem) return null;

        PickableItem itemToDrop = HeldItem;
        HeldItem = null;

        Vector3 dropPos = customDropPosition ?? (transform.position + transform.forward * dropForwardOffset + Vector3.up * 0.2f);
        itemToDrop.OnDroppedToWorld(dropPos, transform.rotation);

        OnDropped?.Invoke(itemToDrop);
        onItemDropped?.Invoke(itemToDrop);
        NotifyTriggerStateChanged();
        return itemToDrop;
    }

    /// <summary>
    /// Checks if this picker can drop its held item.
    /// Drop is only possible if the picker is holding an item
    /// and is near or touching a drop area that has space and can accept it.
    /// </summary>
    public virtual bool CanDrop()
    {
        if (!IsHoldingItem) return false;

        ItemDropArea dropArea = GetClosestDropArea();
        if (dropArea == null || !dropArea.AllowDrop) return false;

        return dropArea.CanAcceptItem(HeldItem);
    }

    /// <summary>
    /// Attempts to place the currently held item into an ItemDropArea.
    /// </summary>
    public virtual bool TryDropTo(ItemDropArea dropArea)
    {
        if (!IsHoldingItem || dropArea == null || !dropArea.AllowDrop) return false;

        // Validated by triggers: either drop area is in picker's trigger, or picker is in drop area's trigger
        bool inTrigger = dropAreasInTrigger.Contains(dropArea) || dropArea.IsPickerInTrigger(this);
        if (!inTrigger) return false;

        if (!dropArea.CanAcceptItem(HeldItem)) return false;

        PickableItem item = HeldItem;
        HeldItem = null;

        bool success = dropArea.TryAddItem(item);
        if (success)
        {
            OnDropped?.Invoke(item);
            onItemDropped?.Invoke(item);
            NotifyTriggerStateChanged();
        }
        else
        {
            // Restore if failed
            HeldItem = item;
        }

        return success;
    }

    /// <summary>
    /// Finds the closest active ItemDropArea detected inside the trigger volume.
    /// </summary>
    public virtual ItemDropArea GetClosestDropArea()
    {
        CleanUpTriggerLists();

        ItemDropArea closestArea = null;
        float minDistanceSqr = float.MaxValue;
        Vector3 myPos = transform.position;

        // 1. Check drop areas detected in this picker's trigger volume
        for (int i = 0; i < dropAreasInTrigger.Count; i++)
        {
            ItemDropArea area = dropAreasInTrigger[i];
            if (area == null) continue;

            float distSqr = (area.transform.position - myPos).sqrMagnitude;
            if (distSqr < minDistanceSqr)
            {
                minDistanceSqr = distSqr;
                closestArea = area;
            }
        }

        // 2. Also check active drop areas whose trigger volume contains this picker
        var allAreas = ItemDropArea.ActiveDropAreas;
        for (int i = 0; i < allAreas.Count; i++)
        {
            ItemDropArea area = allAreas[i];
            if (area == null) continue;

            if (area.IsPickerInTrigger(this))
            {
                float distSqr = (area.transform.position - myPos).sqrMagnitude;
                if (distSqr < minDistanceSqr)
                {
                    minDistanceSqr = distSqr;
                    closestArea = area;
                }
            }
        }

        return closestArea;
    }

    /// <summary>
    /// Context-sensitive interaction:
    /// - If holding an item: places into closest drop area if near or touching one.
    /// - If empty handed: picks from closest drop area or nearest pickable item detected by trigger.
    /// </summary>
    public virtual bool TryInteract()
    {
        if (IsHoldingItem)
        {
            ItemDropArea dropArea = GetClosestDropArea();
            if (dropArea != null && dropArea.CanAcceptItem(HeldItem))
            {
                return TryDropTo(dropArea);
            }

            return false;
        }
        else
        {
            return TryPickClosest();
        }
    }

    public void NotifyTriggerStateChanged()
    {
        OnTriggerStateChanged?.Invoke();
        onTriggerStateChanged?.Invoke();
    }

    private void CleanUpTriggerLists()
    {
        for (int i = itemsInTrigger.Count - 1; i >= 0; i--)
        {
            PickableItem item = itemsInTrigger[i];
            if (item == null || !item.gameObject.activeInHierarchy || item.IsPickedUp)
            {
                itemsInTrigger.RemoveAt(i);
            }
        }

        for (int i = dropAreasInTrigger.Count - 1; i >= 0; i--)
        {
            ItemDropArea area = dropAreasInTrigger[i];
            if (area == null || !area.gameObject.activeInHierarchy)
            {
                dropAreasInTrigger.RemoveAt(i);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (holdPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(holdPoint.position, Vector3.one * 0.2f);
        }
    }
}
