using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dumb View component for the player's contextual interaction UI (buttons and tooltip).
/// Contains no gameplay or physics logic. Exposes clean setters and button click events for the Presenter.
/// </summary>
[DisallowMultipleComponent]
public class PlayerInteractionView : MonoBehaviour
{
    [Header("Buttons")]
    [Tooltip("Button used to trigger item pickup.")]
    [SerializeField] private Button pickButton;

    [Tooltip("Button used to trigger item dropping or placing.")]
    [SerializeField] private Button dropButton;

    [Header("Tooltip")]
    [Tooltip("Text label displaying what item is currently held.")]
    [SerializeField] private TextMeshProUGUI tooltipText;

    [Tooltip("Optional CanvasGroup to smoothly show/hide the tooltip.")]
    [SerializeField] private CanvasGroup tooltipGroup;

    [Tooltip("Optional root GameObject of the tooltip container.")]
    [SerializeField] private GameObject tooltipRoot;

    // Events forwarded to Presenter
    public event Action OnPickClicked;
    public event Action<PickableItem> OnPickSpecificItem;
    public event Action OnDropClicked;

    public Button PickButton => pickButton;
    public Button DropButton => dropButton;
    public TextMeshProUGUI TooltipText => tooltipText;

    // Pooled dynamic buttons for multiple items in drop areas
    private readonly List<Button> spawnedPickButtons = new();
    private PickableItem defaultButtonTargetItem;

    private void Awake()
    {
        RegisterButtonListeners();
    }

    private void OnDestroy()
    {
        UnregisterButtonListeners();
    }

    /// <summary>
    /// Configures pick buttons according to the available items in reach.
    /// If there are multiple items (e.g. at the fridge or a prep table), separate buttons
    /// are shown for each item (e.g. 'Pick Lettuce', 'Pick Cheese', 'Pick Meat').
    /// </summary>
    public void SetPickButtons(IReadOnlyList<PickableItem> items, bool canPick)
    {
        if (pickButton == null) return;

        if (items == null || items.Count <= 1)
        {
            // Single button mode
            pickButton.gameObject.SetActive(true);
            defaultButtonTargetItem = (items != null && items.Count == 1) ? items[0] : null;

            string label = defaultButtonTargetItem != null
                ? $"Pick {defaultButtonTargetItem.ItemName}"
                : "Pick";
            SetButtonLabel(pickButton, label);
            pickButton.interactable = canPick && (defaultButtonTargetItem != null || items != null);

            // Hide extra dynamic buttons
            for (int i = 0; i < spawnedPickButtons.Count; i++)
            {
                if (spawnedPickButtons[i] != null)
                {
                    spawnedPickButtons[i].gameObject.SetActive(false);
                }
            }
        }
        else
        {
            // Multiple buttons mode: one for each available item
            // Button 0: use default pickButton
            pickButton.gameObject.SetActive(true);
            defaultButtonTargetItem = items[0];
            SetButtonLabel(pickButton, $"Pick {items[0].ItemName}");
            pickButton.interactable = canPick;

            // Buttons 1 to N-1
            int neededExtra = items.Count - 1;
            while (spawnedPickButtons.Count < neededExtra)
            {
                Button newBtn = Instantiate(pickButton, pickButton.transform.parent);
                newBtn.name = $"PickButton_Dynamic_{spawnedPickButtons.Count + 1}";
                spawnedPickButtons.Add(newBtn);
            }

            // Ensure correct sibling order right after pickButton
            int baseSiblingIndex = pickButton.transform.GetSiblingIndex();

            for (int i = 0; i < neededExtra; i++)
            {
                Button btn = spawnedPickButtons[i];
                PickableItem item = items[i + 1];

                btn.gameObject.SetActive(true);
                btn.transform.SetSiblingIndex(baseSiblingIndex + i + 1);
                SetButtonLabel(btn, $"Pick {item.ItemName}");
                btn.interactable = canPick;

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    OnPickSpecificItem?.Invoke(item);
                });
            }

            // Hide unused spawned buttons
            for (int i = neededExtra; i < spawnedPickButtons.Count; i++)
            {
                if (spawnedPickButtons[i] != null)
                {
                    spawnedPickButtons[i].gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// Enables or disables interactivity for all Pick buttons.
    /// </summary>
    public void SetPickButtonInteractable(bool interactable)
    {
        if (pickButton != null)
        {
            pickButton.interactable = interactable;
        }

        for (int i = 0; i < spawnedPickButtons.Count; i++)
        {
            if (spawnedPickButtons[i] != null && spawnedPickButtons[i].gameObject.activeSelf)
            {
                spawnedPickButtons[i].interactable = interactable;
            }
        }
    }

    private void SetButtonLabel(Button button, string text)
    {
        if (button == null) return;
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.text = text;
        }
    }

    /// <summary>
    /// Enables or disables interactivity for the Drop button.
    /// </summary>
    public void SetDropButtonInteractable(bool interactable)
    {
        if (dropButton != null)
        {
            dropButton.interactable = interactable;
        }
    }

    /// <summary>
    /// Updates the tooltip message and toggles its visibility.
    /// </summary>
    public void SetTooltip(string message, bool visible)
    {
        if (tooltipText != null)
        {
            tooltipText.text = message;
        }

        if (tooltipGroup != null)
        {
            tooltipGroup.alpha = visible ? 1f : 0f;
            tooltipGroup.blocksRaycasts = visible;
        }
        else if (tooltipRoot != null)
        {
            tooltipRoot.SetActive(visible);
        }
    }

    private void RegisterButtonListeners()
    {
        if (pickButton != null)
        {
            pickButton.onClick.AddListener(HandlePickClicked);
        }

        if (dropButton != null)
        {
            dropButton.onClick.AddListener(HandleDropClicked);
        }
    }

    private void UnregisterButtonListeners()
    {
        if (pickButton != null)
        {
            pickButton.onClick.RemoveListener(HandlePickClicked);
        }

        if (dropButton != null)
        {
            dropButton.onClick.RemoveListener(HandleDropClicked);
        }

        for (int i = 0; i < spawnedPickButtons.Count; i++)
        {
            if (spawnedPickButtons[i] != null)
            {
                spawnedPickButtons[i].onClick.RemoveAllListeners();
            }
        }
    }

    private void HandlePickClicked()
    {
        if (defaultButtonTargetItem != null)
        {
            OnPickSpecificItem?.Invoke(defaultButtonTargetItem);
        }
        else
        {
            OnPickClicked?.Invoke();
        }
    }

    private void HandleDropClicked()
    {
        OnDropClicked?.Invoke();
    }
}
