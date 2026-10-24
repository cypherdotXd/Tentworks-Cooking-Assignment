using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public class UITKOrderPresenter : MonoBehaviour
{
    private class OrderCardView
    {
        public VisualElement CardRoot;
        public Label TitleLabel;
        public Label TimerLabel;
        public List<VisualElement> Badges = new();
        public List<VisualElement> BadgeIcons = new();
    }

    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private List<OrderWindow> _orderWindows = new();

    private List<OrderCardView> _cardViews = new();
    private Action<OrderWindow.Order>[] _spawnedHandlers;
    private Action<OrderWindow.Order>[] _updatedHandlers;
    private Action<OrderWindow.Order>[] _completedHandlers;

    private const int MaxBadgesPerCard = 3;

    void OnEnable()
    {
        VisualElement root = _uiDocument.rootVisualElement;
        if (root == null) return;

        _cardViews.Clear();
        for (int i = 0; i < 4; i++)
        {
            var cardView = new OrderCardView();
            cardView.CardRoot = root.Q<VisualElement>($"orderCard{i}");
            cardView.TitleLabel = root.Q<Label>($"orderTitle{i}");
            cardView.TimerLabel = root.Q<Label>($"orderTimer{i}");

            for (int b = 0; b < MaxBadgesPerCard; b++)
            {
                var badge = root.Q<VisualElement>($"badge{i}_{b}");
                var icon = badge != null ? badge.Q<VisualElement>(className: "badge-icon") : null;
                cardView.Badges.Add(badge);
                cardView.BadgeIcons.Add(icon);
            }

            _cardViews.Add(cardView);
        }

        _spawnedHandlers = new Action<OrderWindow.Order>[_orderWindows.Count];
        _updatedHandlers = new Action<OrderWindow.Order>[_orderWindows.Count];
        _completedHandlers = new Action<OrderWindow.Order>[_orderWindows.Count];

        for (int i = 0; i < _orderWindows.Count; i++)
        {
            int index = i;
            _spawnedHandlers[index] = order => HandleOrderUpdated(index, order);
            _updatedHandlers[index] = order => HandleOrderUpdated(index, order);
            _completedHandlers[index] = order => HandleOrderCompleted(index);

            var window = _orderWindows[index];
            if (window == null) continue;

            window.OnOrderSpawned += _spawnedHandlers[index];
            window.OnOrderUpdated += _updatedHandlers[index];
            window.OnOrderCompleted += _completedHandlers[index];

            if (window.HasActiveOrder)
            {
                HandleOrderUpdated(index, window.CurrentOrder);
            }
            else
            {
                HandleOrderCompleted(index);
            }
        }
    }

    void OnDisable()
    {
        if (_spawnedHandlers == null) return;
        for (int i = 0; i < _orderWindows.Count; i++)
        {
            var window = _orderWindows[i];
            if (window == null || i >= _spawnedHandlers.Length) continue;

            if (_spawnedHandlers[i] != null) window.OnOrderSpawned -= _spawnedHandlers[i];
            if (_updatedHandlers[i] != null) window.OnOrderUpdated -= _updatedHandlers[i];
            if (_completedHandlers[i] != null) window.OnOrderCompleted -= _completedHandlers[i];
        }
    }

    public void RefreshOrder(int windowIndex)
    {
        if (windowIndex < 0 || windowIndex >= _orderWindows.Count) return;
        var window = _orderWindows[windowIndex];
        if (window == null) return;

        if (window.HasActiveOrder)
        {
            HandleOrderUpdated(windowIndex, window.CurrentOrder);
        }
        else
        {
            HandleOrderCompleted(windowIndex);
        }
    }

    private void HandleOrderUpdated(int windowIndex, OrderWindow.Order order)
    {
        if (windowIndex < 0 || windowIndex >= _cardViews.Count || order == null) return;
        var card = _cardViews[windowIndex];
        if (card.CardRoot == null) return;

        card.CardRoot.RemoveFromClassList("hidden");
        card.CardRoot.style.display = DisplayStyle.Flex;

        if (card.TitleLabel != null)
            card.TitleLabel.text = $"ORDER #{order.orderNumber}";

        if (card.TimerLabel != null)
        {
            int minutes = Mathf.FloorToInt(order.elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(order.elapsedTime % 60f);
            card.TimerLabel.text = $"{minutes:00}:{seconds:00}";
        }

        for (int i = 0; i < card.Badges.Count; i++)
        {
            var badge = card.Badges[i];
            var icon = card.BadgeIcons[i];
            if (badge == null) continue;

            if (i < order.items.Count)
            {
                var item = order.items[i];
                badge.RemoveFromClassList("hidden");
                badge.style.display = DisplayStyle.Flex;

                if (item.isFulfilled)
                {
                    badge.AddToClassList("fulfilled");
                }
                else
                {
                    badge.RemoveFromClassList("fulfilled");
                }

                SetBadgeIconType(icon, item.type);
            }
            else
            {
                badge.AddToClassList("hidden");
                badge.style.display = DisplayStyle.None;
            }
        }
    }

    private void HandleOrderCompleted(int windowIndex)
    {
        if (windowIndex < 0 || windowIndex >= _cardViews.Count) return;
        var card = _cardViews[windowIndex];
        if (card.CardRoot == null) return;

        card.CardRoot.AddToClassList("hidden");
        card.CardRoot.style.display = DisplayStyle.None;
    }

    private void SetBadgeIconType(VisualElement icon, Ingredient.IngredientType type)
    {
        if (icon == null) return;

        icon.RemoveFromClassList("badge-vegetable");
        icon.RemoveFromClassList("badge-cheese");
        icon.RemoveFromClassList("badge-meat");

        switch (type)
        {
            case Ingredient.IngredientType.Vegetable:
                icon.AddToClassList("badge-vegetable");
                break;
            case Ingredient.IngredientType.Cheese:
                icon.AddToClassList("badge-cheese");
                break;
            case Ingredient.IngredientType.Meat:
                icon.AddToClassList("badge-meat");
                break;
        }
    }
}
