using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Trash receptacle script that receives dropped items and returns them to the Fridge pool.
/// Holds a reference to its own ItemDropArea and recycles any item placed inside it.
/// </summary>
[DisallowMultipleComponent]
public class Trash : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The ItemDropArea associated with this trash bin/station.")]
    [SerializeField] private ItemDropArea dropArea;

    [Tooltip("The Fridge system whose pool will receive recycled ingredients.")]
    [SerializeField] private Fridge fridge;

    [Header("Events")]
    [Tooltip("Invoked when an item is placed in the trash and recycled.")]
    public UnityEvent<PickableItem> onItemTrashed = new();

    public event Action<PickableItem> OnItemTrashed;

    public ItemDropArea DropArea
    {
        get => dropArea;
        set
        {
            if (dropArea != value)
            {
                if (dropArea != null)
                {
                    dropArea.OnItemAdded -= HandleItemAdded;
                }
                dropArea = value;
                if (dropArea != null && enabled)
                {
                    dropArea.OnItemAdded += HandleItemAdded;
                }
            }
        }
    }

    public Fridge Fridge
    {
        get => fridge;
        set => fridge = value;
    }

    private void Awake()
    {
        if (dropArea == null)
        {
            dropArea = GetComponent<ItemDropArea>();
        }

        if (fridge == null)
        {
            fridge = FindAnyObjectByType<Fridge>();
        }
    }

    private void OnEnable()
    {
        if (dropArea != null)
        {
            dropArea.OnItemAdded += HandleItemAdded;
        }
    }

    private void OnDisable()
    {
        if (dropArea != null)
        {
            dropArea.OnItemAdded -= HandleItemAdded;
        }
    }

    private void Start()
    {
        if (dropArea != null)
        {
            // Ensure dropping items into trash is enabled
            dropArea.AllowDrop = true;

            // Process any starting items that might already be in the drop area
            var currentItems = dropArea.CurrentItems;
            if (currentItems != null && currentItems.Count > 0)
            {
                for (int i = currentItems.Count - 1; i >= 0; i--)
                {
                    TrashItem(currentItems[i]);
                }
            }
        }
    }

    private void HandleItemAdded(PickableItem item)
    {
        if (item == null) return;
        TrashItem(item);
    }

    /// <summary>
    /// Removes the item from the drop area and recycles it back into the fridge pool.
    /// If no fridge reference is available or the item does not belong to a fridge slot, it is safely destroyed.
    /// </summary>
    public void TrashItem(PickableItem item)
    {
        if (item == null) return;

        // 1. Remove from this drop area so the slot remains perpetually open
        if (dropArea != null)
        {
            dropArea.RemoveItem(item);
        }

        // 2. Return to the fridge pool, or destroy if no fridge is present
        if (fridge != null)
        {
            fridge.ReturnToPool(item);
        }
        else
        {
            Destroy(item.gameObject);
        }

        OnItemTrashed?.Invoke(item);
        onItemTrashed?.Invoke(item);
    }
}
