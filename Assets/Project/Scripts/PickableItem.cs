using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Represents a 3D item (e.g. vegetable, ingredient, or prop) that can be picked up,
/// carried by an ItemPicker, and dropped into the world or onto an ItemDropArea.
/// </summary>
[DisallowMultipleComponent]
public class PickableItem : MonoBehaviour
{
    private static readonly List<PickableItem> s_ActivePickables = new();
    public static IReadOnlyList<PickableItem> ActivePickables => s_ActivePickables;

    [Header("Item Info")]
    [Tooltip("Unique or category identifier for the item (e.g. Tomato, Lettuce, Cheese).")]
    [SerializeField] private string itemName = "Vegetable";

    [Header("Carry Alignment")]
    [Tooltip("Local position offset when attached to an ItemPicker's hold point.")]
    [SerializeField] private Vector3 holdPositionOffset = Vector3.zero;

    [Tooltip("Local euler rotation offset when attached to an ItemPicker's hold point.")]
    [SerializeField] private Vector3 holdRotationOffset = Vector3.zero;

    [Header("Placement Alignment")]
    [Tooltip("Local position offset when placed in an ItemDropArea slot.")]
    [SerializeField] private Vector3 dropPositionOffset = Vector3.zero;

    [Header("Unity Events")]
    [Tooltip("Invoked when this item is picked up by a picker.")]
    public UnityEvent<ItemPicker> onPickedUp = new();

    [Tooltip("Invoked when this item is dropped into the world.")]
    public UnityEvent onDropped = new();

    [Tooltip("Invoked when this item is placed into an ItemDropArea.")]
    public UnityEvent<ItemDropArea> onPlacedInDropArea = new();

    [Tooltip("Invoked when the pickable state changes.")]
    public UnityEvent<bool> onPickableChanged = new();

    [Header("Pick Settings")]
    [Tooltip("Whether this item can currently be picked up by an ItemPicker.")]
    [SerializeField] private bool isPickable = true;

    // Cached physics components
    private Rigidbody rb;
    private readonly List<Collider> itemColliders = new();
    private readonly List<Collider> ignoredPlayerColliders = new();

    public string ItemName
    {
        get => itemName;
        set => itemName = value;
    }

    public bool IsPickable
    {
        get => isPickable && enabled;
        set => SetPickable(value);
    }

    public bool IsPickedUp { get; private set; }
    public ItemPicker CurrentPicker { get; private set; }
    public ItemDropArea CurrentDropArea { get; private set; }
    public Rigidbody Rigidbody => rb;
    public IReadOnlyList<Collider> ItemColliders => itemColliders;

    // C# Events
    public event Action<ItemPicker> OnPicked;
    public event Action OnDroppedAction;
    public event Action<ItemDropArea> OnPlacedInArea;
    public event Action<bool> OnPickableChanged;

    /// <summary>
    /// Enables or disables whether this item can be picked up.
    /// </summary>
    public void SetPickable(bool pickable)
    {
        if (isPickable != pickable)
        {
            isPickable = pickable;
            OnPickableChanged?.Invoke(pickable);
            onPickableChanged?.Invoke(pickable);

            if (CurrentDropArea != null)
            {
                CurrentDropArea.NotifyPickersInTrigger();
            }
        }
    }

    private void Awake()
    {
        CacheComponents();
    }

    private void CacheComponents()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        itemColliders.Clear();
        GetComponentsInChildren<Collider>(true, itemColliders);
    }

    private void OnEnable()
    {
        if (!s_ActivePickables.Contains(this))
        {
            s_ActivePickables.Add(this);
        }
        if (CurrentDropArea != null)
        {
            CurrentDropArea.NotifyPickersInTrigger();
        }
    }

    private void OnDisable()
    {
        s_ActivePickables.Remove(this);
        if (CurrentDropArea != null)
        {
            CurrentDropArea.NotifyPickersInTrigger();
        }
    }

    /// <summary>
    /// Called by an ItemPicker when this item is picked up.
    /// Attaches the item to the picker's hold point and completely disables its physics/colliders
    /// so it cannot collide with the player, stations, or floor while carried.
    /// </summary>
    public virtual void OnPickedUp(ItemPicker picker, Transform holdPoint)
    {
        if (IsPickedUp && CurrentPicker != null)
        {
            CurrentPicker.Drop();
        }

        // If previously resting on a drop area, detach from it
        if (CurrentDropArea != null)
        {
            CurrentDropArea.RemoveItem(this);
            CurrentDropArea = null;
        }

        IsPickedUp = true;
        CurrentPicker = picker;

        // Parent to hold point
        Transform targetParent = holdPoint != null ? holdPoint : picker.transform;
        transform.SetParent(targetParent);
        transform.localPosition = holdPositionOffset;
        transform.localRotation = Quaternion.Euler(holdRotationOffset);

        CacheComponents();

        // 1. Disable all colliders on this item and all children (prevents player physics depenetration impulses)
        for (int i = 0; i < itemColliders.Count; i++)
        {
            if (itemColliders[i] != null)
            {
                itemColliders[i].enabled = false;
            }
        }

        // 2. Explicitly ignore collision against all player colliders as a secondary safeguard
        ignoredPlayerColliders.Clear();
        if (picker != null)
        {
            Collider[] playerColliders = picker.GetComponentsInParent<Collider>(true);
            for (int p = 0; p < playerColliders.Length; p++)
            {
                Collider pCol = playerColliders[p];
                if (pCol == null) continue;

                for (int i = 0; i < itemColliders.Count; i++)
                {
                    Collider iCol = itemColliders[i];
                    if (iCol != null)
                    {
                        Physics.IgnoreCollision(pCol, iCol, true);
                    }
                }
                ignoredPlayerColliders.Add(pCol);
            }
        }

        // 3. Freeze Rigidbody and turn off collision detection while carried
        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        OnPicked?.Invoke(picker);
        onPickedUp?.Invoke(picker);
    }

    /// <summary>
    /// Called when the item is dropped freely into the world.
    /// Unparents the item and restores its physics and colliders.
    /// </summary>
    public virtual void OnDroppedToWorld(Vector3 dropPosition, Quaternion dropRotation)
    {
        IsPickedUp = false;
        CurrentPicker = null;
        CurrentDropArea = null;

        transform.SetParent(null);
        transform.position = dropPosition;
        transform.rotation = dropRotation;

        CacheComponents();

        // 1. Re-enable all colliders on this item
        for (int i = 0; i < itemColliders.Count; i++)
        {
            if (itemColliders[i] != null)
            {
                itemColliders[i].enabled = true;
            }
        }

        // 2. Restore collision against player colliders
        for (int p = 0; p < ignoredPlayerColliders.Count; p++)
        {
            Collider pCol = ignoredPlayerColliders[p];
            if (pCol == null) continue;

            for (int i = 0; i < itemColliders.Count; i++)
            {
                Collider iCol = itemColliders[i];
                if (iCol != null)
                {
                    Physics.IgnoreCollision(pCol, iCol, false);
                }
            }
        }
        ignoredPlayerColliders.Clear();

        // 3. Re-enable dynamic physics simulation
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        OnDroppedAction?.Invoke();
        onDropped?.Invoke();
    }

    /// <summary>
    /// Called when the item is placed into an ItemDropArea (e.g. prep table or stove).
    /// </summary>
    public virtual void OnPlacedInDropArea(ItemDropArea dropArea, Transform slotTransform)
    {
        IsPickedUp = false;
        CurrentPicker = null;
        CurrentDropArea = dropArea;

        if (slotTransform != null)
        {
            transform.SetParent(slotTransform);
            transform.localPosition = dropPositionOffset;
            transform.localRotation = Quaternion.identity;
        }
        else
        {
            transform.SetParent(dropArea.transform);
            transform.localPosition = dropPositionOffset;
            transform.localRotation = Quaternion.identity;
        }

        CacheComponents();

        // 1. Re-enable colliders so picker triggers can detect this item on the station
        for (int i = 0; i < itemColliders.Count; i++)
        {
            if (itemColliders[i] != null)
            {
                itemColliders[i].enabled = true;
            }
        }

        // 2. Restore collision against player colliders
        for (int p = 0; p < ignoredPlayerColliders.Count; p++)
        {
            Collider pCol = ignoredPlayerColliders[p];
            if (pCol == null) continue;

            for (int i = 0; i < itemColliders.Count; i++)
            {
                Collider iCol = itemColliders[i];
                if (iCol != null)
                {
                    Physics.IgnoreCollision(pCol, iCol, false);
                }
            }
        }
        ignoredPlayerColliders.Clear();

        // 3. Keep kinematic in the station slot
        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
            rb.detectCollisions = true;
        }

        OnPlacedInArea?.Invoke(dropArea);
        onPlacedInDropArea?.Invoke(dropArea);
    }
}
