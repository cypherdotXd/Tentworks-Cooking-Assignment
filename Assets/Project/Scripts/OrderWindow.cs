using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class OrderWindow : MonoBehaviour
{
    [SerializeField] private ItemDropArea dropArea;
    [SerializeField] private float newOrderDelay = 5f;
    [SerializeField] private bool requirePrepared = true;
    [SerializeField] private Fridge fridge;

    [System.Serializable]
    public class OrderItem
    {
        public Ingredient.IngredientType type;
        public bool isFulfilled;
    }

    [System.Serializable]
    public class Order
    {
        public int orderNumber;
        public List<OrderItem> items = new();
        public float elapsedTime;
        public bool isCompleted;
    }

    private Order currentOrder;
    private float delayTimer;
    private bool isWaitingForNextOrder;
    private int orderCounter;

    public Order CurrentOrder => currentOrder;
    public bool HasActiveOrder => currentOrder != null && !currentOrder.isCompleted;
    public bool IsWaitingForNextOrder => isWaitingForNextOrder;
    public float DelayTimer => delayTimer;
    public ItemDropArea DropArea => dropArea;

    public event Action<Order> OnOrderSpawned;
    public event Action<Order> OnOrderUpdated;
    public event Action<Order> OnOrderCompleted;

    private void Awake()
    {
        if (!dropArea) dropArea = GetComponent<ItemDropArea>();
        if (!fridge) fridge = FindAnyObjectByType<Fridge>();
    }

    private void OnEnable()
    {
        if (dropArea)
        {
            dropArea.OnItemAdded += OnItemAdded;
            dropArea.ItemFilter = CanAcceptItem;
        }
    }

    private void OnDisable()
    {
        if (dropArea)
        {
            dropArea.OnItemAdded -= OnItemAdded;
            if (dropArea.ItemFilter == CanAcceptItem) dropArea.ItemFilter = null;
        }
    }

    private void Start()
    {
        SpawnNewOrder();
    }

    private void Update()
    {
        if (HasActiveOrder)
        {
            currentOrder.elapsedTime += Time.deltaTime;
            OnOrderUpdated?.Invoke(currentOrder);
        }
        else if (isWaitingForNextOrder)
        {
            delayTimer -= Time.deltaTime;
            if (delayTimer <= 0f)
            {
                isWaitingForNextOrder = false;
                SpawnNewOrder();
            }
        }
    }

    public void SpawnNewOrder()
    {
        orderCounter++;
        currentOrder = new Order
        {
            orderNumber = orderCounter,
            elapsedTime = 0f,
            isCompleted = false
        };

        // 50% chance for 2 ingredients, 50% for 3 ingredients
        int count = UnityEngine.Random.value < 0.5f ? 2 : 3;
        for (int i = 0; i < count; i++)
        {
            var randomType = (Ingredient.IngredientType)UnityEngine.Random.Range(0, 3);
            currentOrder.items.Add(new OrderItem { type = randomType, isFulfilled = false });
        }

        if (dropArea) dropArea.AllowDrop = true;
        OnOrderSpawned?.Invoke(currentOrder);
    }

    private void OnItemAdded(PickableItem item)
    {
        if (!HasActiveOrder || !item) return;

        var ingredient = item.GetComponent<Ingredient>();
        if (!ingredient || !IsValidIngredient(ingredient)) return;

        // Find first unfulfilled requirement of this type
        OrderItem match = null;
        for (int i = 0; i < currentOrder.items.Count; i++)
        {
            if (!currentOrder.items[i].isFulfilled && currentOrder.items[i].type == ingredient.Type)
            {
                match = currentOrder.items[i];
                break;
            }
        }

        if (match != null)
        {
            match.isFulfilled = true;

            // Remove from drop area and recycle/destroy
            if (dropArea) dropArea.RemoveItem(item);
            if (fridge) fridge.ReturnToPool(item);
            else Destroy(item.gameObject);

            OnOrderUpdated?.Invoke(currentOrder);

            if (IsOrderFulfilled())
            {
                CompleteOrder();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Direct trigger detection fallback if not using ItemDropArea
        if (!dropArea)
        {
            var item = other.GetComponentInParent<PickableItem>();
            if (item && !item.IsPickedUp)
            {
                OnItemAdded(item);
            }
        }
    }

    private bool IsOrderFulfilled()
    {
        if (currentOrder == null || currentOrder.items.Count == 0) return false;
        for (int i = 0; i < currentOrder.items.Count; i++)
        {
            if (!currentOrder.items[i].isFulfilled) return false;
        }
        return true;
    }

    private void CompleteOrder()
    {
        currentOrder.isCompleted = true;
        isWaitingForNextOrder = true;
        delayTimer = newOrderDelay;
        if (dropArea) dropArea.AllowDrop = false;

        OnOrderCompleted?.Invoke(currentOrder);
    }

    private bool CanAcceptItem(PickableItem item)
    {
        if (!HasActiveOrder || !item) return false;

        var ingredient = item.GetComponent<Ingredient>();
        if (!ingredient || !IsValidIngredient(ingredient)) return false;

        for (int i = 0; i < currentOrder.items.Count; i++)
        {
            if (!currentOrder.items[i].isFulfilled && currentOrder.items[i].type == ingredient.Type)
                return true;
        }

        return false;
    }

    private bool IsValidIngredient(Ingredient ingredient)
    {
        if (!requirePrepared) return true;
        if (ingredient.Type == Ingredient.IngredientType.Vegetable) return !ingredient.IsRaw;
        if (ingredient.Type == Ingredient.IngredientType.Meat) return !ingredient.IsRaw;
        return true;
    }
}
