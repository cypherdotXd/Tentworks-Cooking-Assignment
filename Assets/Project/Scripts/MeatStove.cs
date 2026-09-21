using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class MeatStove : MonoBehaviour
{
    [SerializeField] private ItemDropArea dropArea;
    [SerializeField] private TMP_Text slot1TimerText;
    [SerializeField] private TMP_Text slot2TimerText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private float cookDuration = 6f;

    private class CookingMeat
    {
        public PickableItem item;
        public Ingredient ingredient;
        public int slotIndex;
        public float timer;
    }

    private readonly List<CookingMeat> cookingMeats = new();

    public ItemDropArea DropArea => dropArea;
    public int CookingCount => cookingMeats.Count;
    public bool IsCooking => cookingMeats.Count > 0;

    private void Awake()
    {
        if (!dropArea) dropArea = GetComponent<ItemDropArea>();
        if (!slot1TimerText && timerText) slot1TimerText = timerText;
    }

    private void OnEnable()
    {
        if (dropArea)
        {
            dropArea.OnItemAdded += OnItemAdded;
            dropArea.OnItemRemoved += OnItemRemoved;
            dropArea.ItemFilter = IsMeat;
        }
    }

    private void OnDisable()
    {
        if (dropArea)
        {
            dropArea.OnItemAdded -= OnItemAdded;
            dropArea.OnItemRemoved -= OnItemRemoved;
            if (dropArea.ItemFilter == IsMeat) dropArea.ItemFilter = null;
        }
    }

    private void Start()
    {
        if (!dropArea) return;

        var items = dropArea.CurrentItems;
        for (int i = 0; i < items.Count; i++)
        {
            OnItemAdded(items[i]);
        }
    }

    private void Update()
    {
        if (cookingMeats.Count == 0) return;

        for (int i = cookingMeats.Count - 1; i >= 0; i--)
        {
            var cooking = cookingMeats[i];
            cooking.timer -= Time.deltaTime;

            if (cooking.timer <= 0f)
            {
                FinishCook(cooking);
                cookingMeats.RemoveAt(i);
            }
        }

        UpdateTimerUI();
    }

    private void OnItemAdded(PickableItem item)
    {
        var ingredient = item ? item.GetComponent<Ingredient>() : null;
        if (!ingredient || ingredient.Type != Ingredient.IngredientType.Meat) return;

        if (ingredient.IsRaw && !IsAlreadyCooking(item))
        {
            StartCook(item, ingredient);
        }
    }

    private void OnItemRemoved(PickableItem item)
    {
        for (int i = cookingMeats.Count - 1; i >= 0; i--)
        {
            if (cookingMeats[i].item == item)
            {
                cookingMeats.RemoveAt(i);
                break;
            }
        }

        UpdateTimerUI();
    }

    private void StartCook(PickableItem item, Ingredient ingredient)
    {
        item.IsPickable = false;
        int slot = GetSlotIndex(item);

        cookingMeats.Add(new CookingMeat
        {
            item = item,
            ingredient = ingredient,
            slotIndex = slot,
            timer = cookDuration
        });

        if (dropArea) dropArea.NotifyPickersInTrigger();
        UpdateTimerUI();
    }

    private void FinishCook(CookingMeat cooking)
    {
        if (cooking.ingredient) cooking.ingredient.Process();
        if (cooking.item) cooking.item.IsPickable = true;

        if (dropArea) dropArea.NotifyPickersInTrigger();
    }

    private void UpdateTimerUI()
    {
        for (int i = 0; i < cookingMeats.Count; i++)
        {
            var cooking = cookingMeats[i];
            TMP_Text text = GetTimer(cooking.slotIndex);
            if(text) text.text = $"{Mathf.Max(0f, cooking.timer):0.0}s";
        }
    }

    private TMP_Text GetTimer(int slotIndex)
    {
        if (slotIndex == 0)
        {
            if (slot1TimerText) return slot1TimerText;
        }
        else if (slotIndex == 1)
        {
            if (slot2TimerText) return slot2TimerText;
        }

        return null;
    }

    private int GetSlotIndex(PickableItem item)
    {
        if (!item || !dropArea || dropArea.DropSlots == null) return -1;

        for (int i = 0; i < dropArea.DropSlots.Count; i++)
        {
            Transform slot = dropArea.DropSlots[i];
            if (slot && (item.transform.parent == slot || item.transform.IsChildOf(slot)))
                return i;
        }

        var items = dropArea.CurrentItems;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == item) return i;
        }

        return -1;
    }

    private bool IsMeat(PickableItem item)
    {
        return item && item.GetComponent<Ingredient>()?.Type == Ingredient.IngredientType.Meat;
    }

    private bool IsAlreadyCooking(PickableItem item)
    {
        for (int i = 0; i < cookingMeats.Count; i++)
        {
            if (cookingMeats[i].item == item) return true;
        }
        return false;
    }
}
