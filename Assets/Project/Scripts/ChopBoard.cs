using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class ChopBoard : MonoBehaviour
{
    [SerializeField] private ItemDropArea dropArea;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private float chopDuration = 2f;

    private PickableItem currentItem;
    private Ingredient currentIngredient;
    private float timer;
    private bool isChopping;
    private bool playerInTrigger;

    public ItemDropArea DropArea => dropArea;
    public bool IsChopping => isChopping;
    public float RemainingTime => timer;

    private void Awake()
    {
        if (!dropArea) dropArea = GetComponent<ItemDropArea>();
    }

    private void OnEnable()
    {
        if (dropArea)
        {
            dropArea.OnItemAdded += OnItemAdded;
            dropArea.OnItemRemoved += OnItemRemoved;
            dropArea.ItemFilter = IsVegetable;
        }
    }

    private void OnDisable()
    {
        if (dropArea)
        {
            dropArea.OnItemAdded -= OnItemAdded;
            dropArea.OnItemRemoved -= OnItemRemoved;
            if (dropArea.ItemFilter == IsVegetable) dropArea.ItemFilter = null;
        }
    }

    private void Start()
    {
        if (dropArea && dropArea.CurrentItems.Count > 0)
            OnItemAdded(dropArea.CurrentItems[0]);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponentInParent<PlayerPickerManager>())
            playerInTrigger = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponentInParent<PlayerPickerManager>())
            playerInTrigger = false;
    }

    private void Update()
    {
        if (!isChopping) return;

        bool isPlayerNear = playerInTrigger || (dropArea && dropArea.HasPickerInTrigger);
        if (!isPlayerNear) return;

        timer -= Time.deltaTime;
        UpdateTimerUI();

        if (timer <= 0f)
            FinishChop();
    }

    private void OnItemAdded(PickableItem item)
    {
        var ingredient = item ? item.GetComponent<Ingredient>() : null;
        if (!ingredient || ingredient.Type != Ingredient.IngredientType.Vegetable) return;

        currentItem = item;
        currentIngredient = ingredient;
        if (dropArea) dropArea.AllowDrop = false;

        if (ingredient.IsRaw)
        {
            timer = chopDuration;
            isChopping = true;
            item.IsPickable = false;
            if (dropArea) dropArea.NotifyPickersInTrigger();
            UpdateTimerUI();
        }
    }

    private void OnItemRemoved(PickableItem item)
    {
        if (item == currentItem)
        {
            isChopping = false;
            currentItem = null;
            currentIngredient = null;
            if (dropArea) dropArea.AllowDrop = true;
            UpdateTimerUI();
        }
    }

    private void FinishChop()
    {
        isChopping = false;
        if (currentIngredient) currentIngredient.Process();
        if (currentItem) currentItem.IsPickable = true;

        if (dropArea) dropArea.NotifyPickersInTrigger();
        UpdateTimerUI();
    }

    private void UpdateTimerUI()
    {
        if (!timerText) return;

        timerText.gameObject.SetActive(isChopping);
        if (isChopping)
        {
            timerText.text = $"{Mathf.Max(0f, timer):0.0}s";
        }
    }

    private bool IsVegetable(PickableItem item)
    {
        return item && item.GetComponent<Ingredient>()?.Type == Ingredient.IngredientType.Vegetable;
    }
}
