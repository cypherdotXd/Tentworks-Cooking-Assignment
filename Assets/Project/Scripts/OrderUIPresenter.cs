using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class OrderUIPresenter : MonoBehaviour
{
    [System.Serializable]
    public class BadgeSlot
    {
        public GameObject root;
        public Image iconImage;
        public GameObject outlineObject;
        public Outline outlineComponent;
    }

    [SerializeField] private OrderWindow orderWindow;
    [SerializeField] private GameObject orderCardRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Transform badgesContainer;
    [SerializeField] private List<BadgeSlot> badges = new();

    [Header("Sprites")]
    [SerializeField] private Sprite vegetableSprite;
    [SerializeField] private Sprite cheeseSprite;
    [SerializeField] private Sprite meatSprite;

    [Header("Colors")]
    [SerializeField] private Color fulfilledOutlineColor = new Color(0.2f, 0.85f, 0.3f, 1f);

    public OrderWindow OrderWindow { get => orderWindow; set => orderWindow = value; }

    private void Awake()
    {
        if (!orderWindow) orderWindow = FindAnyObjectByType<OrderWindow>();
        if (!orderCardRoot) orderCardRoot = gameObject;
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        DiscoverBadges();
    }

    private void OnEnable()
    {
        HookEvents();
        if (orderWindow && orderWindow.CurrentOrder != null)
        {
            if (orderWindow.HasActiveOrder)
            {
                SetCardVisible(true);
                RefreshUI(orderWindow.CurrentOrder);
            }
            else
            {
                SetCardVisible(false);
            }
        }
    }

    private void OnDisable()
    {
        UnhookEvents();
    }

    private void HookEvents()
    {
        if (orderWindow)
        {
            orderWindow.OnOrderSpawned -= HandleOrderSpawned;
            orderWindow.OnOrderSpawned += HandleOrderSpawned;

            orderWindow.OnOrderUpdated -= HandleOrderUpdated;
            orderWindow.OnOrderUpdated += HandleOrderUpdated;

            orderWindow.OnOrderCompleted -= HandleOrderCompleted;
            orderWindow.OnOrderCompleted += HandleOrderCompleted;
        }
    }

    private void UnhookEvents()
    {
        if (orderWindow)
        {
            orderWindow.OnOrderSpawned -= HandleOrderSpawned;
            orderWindow.OnOrderUpdated -= HandleOrderUpdated;
            orderWindow.OnOrderCompleted -= HandleOrderCompleted;
        }
    }

    private void HandleOrderSpawned(OrderWindow.Order order)
    {
        SetCardVisible(true);
        RefreshUI(order);
    }

    private void HandleOrderUpdated(OrderWindow.Order order)
    {
        RefreshUI(order);
    }

    private void HandleOrderCompleted(OrderWindow.Order order)
    {
        SetCardVisible(false);
    }

    private void SetCardVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        if (orderCardRoot && orderCardRoot != gameObject)
        {
            orderCardRoot.SetActive(visible);
        }
    }

    private void DiscoverBadges()
    {
        if (badgesContainer == null) return;

        badges.Clear();
        for (int i = 0; i < badgesContainer.childCount; i++)
        {
            var child = badgesContainer.GetChild(i).gameObject;
            RegisterBadge(child);
        }
    }

    private void RegisterBadge(GameObject badgeObj)
    {
        // 1. Find Icon Image specifically
        Image icon = null;
        Transform iconT = badgeObj.transform.Find("Icon");
        if (iconT != null)
        {
            icon = iconT.GetComponent<Image>();
        }

        if (icon == null)
        {
            var images = badgeObj.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i].gameObject != badgeObj && !images[i].name.Equals("Outline", StringComparison.OrdinalIgnoreCase))
                {
                    icon = images[i];
                    break;
                }
            }
            if (icon == null && images.Length > 1)
            {
                icon = images[images.Length - 1];
            }
        }

        // 2. Find Outline child object or Outline component
        GameObject outlineObj = null;
        Transform outlineT = badgeObj.transform.Find("Outline");
        if (outlineT != null)
        {
            outlineObj = outlineT.gameObject;
            outlineObj.SetActive(false);
        }
        else if (badges.Count > 0 && badges[0].outlineObject != null)
        {
            var clonedOutline = Instantiate(badges[0].outlineObject, badgeObj.transform);
            clonedOutline.name = "Outline";
            clonedOutline.SetActive(false);
            outlineObj = clonedOutline;
        }

        Outline outlineComp = badgeObj.GetComponent<Outline>();
        if (outlineObj == null && outlineComp == null)
        {
            outlineComp = badgeObj.AddComponent<Outline>();
            outlineComp.effectColor = fulfilledOutlineColor;
            outlineComp.effectDistance = new Vector2(3f, -3f);
            outlineComp.enabled = false;
        }

        // CRITICAL: Ensure outline is rendered behind icon, and icon is ALWAYS the topmost sibling!
        if (outlineObj != null)
        {
            outlineObj.transform.SetAsFirstSibling();
        }
        if (icon != null)
        {
            icon.transform.SetAsLastSibling();
            icon.gameObject.SetActive(true);
        }

        badges.Add(new BadgeSlot
        {
            root = badgeObj,
            iconImage = icon,
            outlineObject = outlineObj,
            outlineComponent = outlineComp
        });
    }

    private void RefreshUI(OrderWindow.Order order)
    {
        if (order == null) return;

        // 1. Update Title
        if (titleText)
        {
            titleText.text = $"ORDER #{order.orderNumber}";
        }

        // 2. Update Timer (format: 00:15)
        if (timerText)
        {
            int minutes = Mathf.FloorToInt(order.elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(order.elapsedTime % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }

        // 3. Ensure enough badges exist for the order items (2 or 3)
        EnsureBadgeCount(order.items.Count);

        // 4. Update Badges
        for (int i = 0; i < badges.Count; i++)
        {
            var badge = badges[i];
            if (badge == null || !badge.root) continue;

            if (i < order.items.Count)
            {
                var item = order.items[i];
                badge.root.SetActive(true);

                if (badge.iconImage)
                {
                    badge.iconImage.gameObject.SetActive(true);
                    badge.iconImage.sprite = GetSpriteForType(item.type);
                    badge.iconImage.transform.SetAsLastSibling();
                }

                if (badge.outlineObject)
                {
                    badge.outlineObject.SetActive(item.isFulfilled);
                }

                if (badge.outlineComponent)
                {
                    badge.outlineComponent.enabled = item.isFulfilled;
                }
            }
            else
            {
                badge.root.SetActive(false);
            }
        }
    }

    private void EnsureBadgeCount(int neededCount)
    {
        if (badges.Count >= neededCount || badges.Count == 0) return;

        var template = badges[0];
        if (!template.root) return;

        Transform parent = template.root.transform.parent;
        while (badges.Count < neededCount)
        {
            var clone = Instantiate(template.root, parent);
            clone.name = $"Badge {badges.Count + 1}";

            if (parent.GetComponent<LayoutGroup>() == null && clone.transform is RectTransform cloneRt && badges.Count > 0)
            {
                var prevRt = badges[badges.Count - 1].root.transform as RectTransform;
                if (prevRt != null)
                {
                    float spacing = 40f;
                    if (badges.Count >= 2 && badges[0].root.transform is RectTransform rt0 && badges[1].root.transform is RectTransform rt1)
                    {
                        spacing = rt1.anchoredPosition.x - rt0.anchoredPosition.x;
                    }
                    cloneRt.anchoredPosition = new Vector2(prevRt.anchoredPosition.x + spacing, prevRt.anchoredPosition.y);
                }
            }

            RegisterBadge(clone);
        }
    }

    private Sprite GetSpriteForType(Ingredient.IngredientType type)
    {
        return type switch
        {
            Ingredient.IngredientType.Vegetable => vegetableSprite,
            Ingredient.IngredientType.Cheese => cheeseSprite,
            Ingredient.IngredientType.Meat => meatSprite,
            _ => null
        };
    }
}
