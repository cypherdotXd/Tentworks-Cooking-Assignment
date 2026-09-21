using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Fridge : MonoBehaviour
{
    [SerializeField] private ItemDropArea dropArea;
    [SerializeField] private List<PickableItem> ingredientPrefabs = new();

    private const int PoolSize = 10;
    private const int MaxInSlot = 3;
    private const float StackSpacing = 0.15f;

    private Transform poolContainer;
    private Queue<PickableItem>[] pools;
    private List<PickableItem>[] activeInSlots;

    public ItemDropArea DropArea => dropArea;
    public List<PickableItem> IngredientPrefabs => ingredientPrefabs;

    private void Awake()
    {
        if (!dropArea) dropArea = GetComponent<ItemDropArea>();
    }

    private void OnEnable()
    {
        if (dropArea) dropArea.OnItemRemoved += OnItemTaken;
    }

    private void OnDisable()
    {
        if (dropArea) dropArea.OnItemRemoved -= OnItemTaken;
    }

    private void Start()
    {
        if (!dropArea) return;

        dropArea.AllowDrop = false;

        GameObject container = new GameObject("_FridgePool");
        container.transform.SetParent(transform);
        container.SetActive(false);
        poolContainer = container.transform;

        int slotCount = Mathf.Min(dropArea.DropSlots.Count, ingredientPrefabs.Count);
        pools = new Queue<PickableItem>[slotCount];
        activeInSlots = new List<PickableItem>[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            pools[i] = new Queue<PickableItem>();
            activeInSlots[i] = new List<PickableItem>();

            for (int p = 0; p < PoolSize; p++)
            {
                pools[i].Enqueue(CreateItem(i));
            }

            for (int s = 0; s < MaxInSlot; s++)
            {
                SpawnItem(i);
            }
        }

        dropArea.NotifyPickersInTrigger();
    }

    private PickableItem CreateItem(int slotIndex)
    {
        PickableItem item = Instantiate(ingredientPrefabs[slotIndex], poolContainer);
        item.name = $"{ingredientPrefabs[slotIndex].ItemName}_Pooled";
        item.gameObject.SetActive(false);
        return item;
    }

    private void SpawnItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= dropArea.DropSlots.Count) return;

        PickableItem item = pools[slotIndex].Count > 0 ? pools[slotIndex].Dequeue() : CreateItem(slotIndex);
        Transform slot = dropArea.DropSlots[slotIndex];

        activeInSlots[slotIndex].Add(item);
        item.gameObject.SetActive(true);
        dropArea.RegisterPlacedItem(item, slot);

        RealignStack(slotIndex);
    }

    private void OnItemTaken(PickableItem item)
    {
        if (!item || activeInSlots == null) return;

        for (int i = 0; i < activeInSlots.Length; i++)
        {
            if (activeInSlots[i].Remove(item))
            {
                RealignStack(i);
                if (activeInSlots[i].Count < MaxInSlot)
                {
                    SpawnItem(i);
                }
                dropArea.NotifyPickersInTrigger();
                break;
            }
        }
    }

    private void RealignStack(int slotIndex)
    {
        var list = activeInSlots[slotIndex];
        Transform slot = dropArea.DropSlots[slotIndex];

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] && list[i].transform.parent == slot)
            {
                list[i].transform.localPosition = new Vector3(0, StackSpacing * i, 0);
                list[i].transform.localRotation = Quaternion.identity;
                list[i].IsPickable = (i == list.Count - 1);
            }
        }
    }

    public void ReturnToPool(PickableItem item)
    {
        if (!item || ingredientPrefabs == null) return;

        var ingredient = item.GetComponent<Ingredient>();
        if (ingredient) ingredient.SetRaw();

        for (int i = 0; i < ingredientPrefabs.Count; i++)
        {
            if (ingredientPrefabs[i] && (item.ItemName.Contains(ingredientPrefabs[i].ItemName) || (ingredient && ingredient.IngredientName == ingredientPrefabs[i].ItemName)))
            {
                if (activeInSlots != null && i < activeInSlots.Length) activeInSlots[i].Remove(item);
                if (dropArea) dropArea.RemoveItem(item);

                item.gameObject.SetActive(false);
                item.transform.SetParent(poolContainer);
                item.transform.localPosition = Vector3.zero;
                if (pools != null && i < pools.Length) pools[i].Enqueue(item);
                return;
            }
        }

        Destroy(item.gameObject);
    }
}
