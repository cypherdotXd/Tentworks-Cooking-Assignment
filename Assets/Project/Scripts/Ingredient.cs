using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Represents an ingredient in the cooking system (Vegetables, Cheese, Meat).
/// Handles visual model states (raw vs chopped/cooked) and provides direct access
/// to enable or disable the attached PickableItem component.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PickableItem))]
public class Ingredient : MonoBehaviour
{
    public enum IngredientType
    {
        Vegetable,
        Cheese,
        Meat
    }

    public enum IngredientState
    {
        Raw,
        Processed
    }

    [Header("Ingredient Definition")]
    [SerializeField] private IngredientType ingredientType = IngredientType.Vegetable;
    [SerializeField] private string ingredientName = "Ingredient";
    [SerializeField] private IngredientState currentState = IngredientState.Raw;
    [SerializeField] private GameObject rawModel;
    [SerializeField] private GameObject processedModel;

    [SerializeField] private PickableItem pickableItem; 

    public IngredientType Type
    {
        get => ingredientType;
        set
        {
            ingredientType = value;
            UpdateVisuals();
        }
    }

    public string IngredientName
    {
        get => ingredientName;
        set
        {
            ingredientName = value;
            UpdateItemName();
        }
    }

    public IngredientState CurrentState => currentState;
    public PickableItem PickableItem => pickableItem;
    public GameObject RawModel => rawModel;
    public GameObject ProcessedModel => processedModel;
    public bool IsRaw => currentState == IngredientState.Raw;
    public bool IsProcessed => currentState == IngredientState.Processed;

    // C# Events
    public event Action<IngredientState> OnStateChanged;
    public event Action<bool> OnPickableStateChanged;

    private void Awake()
    {
        UpdateVisuals();
    }



    #region State & Visual Management

    /// <summary>
    /// Sets the processing state of this ingredient and activates the appropriate 3D model.
    /// </summary>
    public void SetState(IngredientState newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            UpdateVisuals();
            OnStateChanged?.Invoke(currentState);
        }
    }
    
    public void Process()
    {
        if (ingredientType == IngredientType.Vegetable || ingredientType == IngredientType.Meat)
        {
            SetState(IngredientState.Processed);
        }
        else
        {
            Debug.LogWarning($"[Ingredient] Cannot process ingredient '{ingredientName}'.", this);
        }
    }

    public void SetRaw()
    {
        SetState(IngredientState.Raw);
    }

    public void UpdateVisuals()
    {
        rawModel.SetActive(currentState == IngredientState.Raw);
        processedModel.SetActive(currentState == IngredientState.Processed);
        
        UpdateItemName();
    }

    private void UpdateItemName()
    {
        if (pickableItem == null) return;

        switch (ingredientType)
        {
            case IngredientType.Vegetable:
                pickableItem.ItemName = currentState == IngredientState.Processed
                    ? $"Chopped {ingredientName}"
                    : ingredientName;
                break;

            case IngredientType.Meat:
                pickableItem.ItemName = currentState == IngredientState.Processed
                    ? $"Cooked {ingredientName}"
                    : ingredientName;
                break;

            case IngredientType.Cheese:
            default:
                pickableItem.ItemName = ingredientName;
                break;
        }
    }

    #endregion

    #region Pick Component Access (Enable / Disable)

    /// <summary>
    /// Whether the attached PickableItem component is enabled.
    /// </summary>
    public bool IsPickComponentEnabled
    {
        get => pickableItem != null && pickableItem.enabled;
        set => TogglePicking(value);
    }

    /// <summary>
    /// Enables or disables the attached PickableItem component directly.
    /// </summary>
    public void TogglePicking(bool isEnabled)
    {
        pickableItem.enabled = isEnabled;
        OnPickableStateChanged?.Invoke(isEnabled);
    }

    /// <summary>
    /// Whether this ingredient is currently allowed to be picked up by an ItemPicker.
    /// </summary>
    public bool IsPickable => pickableItem.enabled;

    #endregion
}
