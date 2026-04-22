using System.Collections.Generic;
using System.Linq;
using Game.Gameplay.Demo1;
using Game.Gameplay.Demo1.System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Gameplay.Demo1.UI.Widget
{
    public class Widget_ShopZone : Widget_SlotZoneBase
    {
        [SerializeField] private Widget_CardView _cardPrefab;
        [SerializeField] private bool _destroyOnBuy = true;

        private readonly List<ShopItem> _items = new List<ShopItem>();

        public global::System.Action<CardModel> OnBuy;

        public void SetCapacity(int capacity)
        {
            SetCapacityInternal(capacity);
            PackToFit();
            LayoutAll();
        }

        public void LoadCards(IEnumerable<CardModel> cards)
        {
            ClearAllCards();
            if (cards == null)
                return;

            foreach (var card in cards)
            {
                AddShopCard(card);
            }
        }

        public void ClearAllCards()
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var view = _items[i].View;
                if (view != null)
                    UnityEngine.Object.Destroy(view.gameObject);
            }
            _items.Clear();
        }

        public Widget_CardView AddShopCard(CardModel model, int? startIndex = null)
        {
            if (_cardPrefab == null || model == null)
                return null;

            var view = UnityEngine.Object.Instantiate(_cardPrefab, transform);
            view.Model = model;
            view.Draggable = false;
            view.OwnerZone = null;
            view.Clicked = OnCardClicked;

            int widthInCells = Mathf.Clamp(view.WidthInCells, 1, 3);
            int desiredStart = startIndex ?? FindFirstFit(widthInCells);
            if (desiredStart < 0)
            {
                UnityEngine.Object.Destroy(view.gameObject);
                return null;
            }

            var item = new ShopItem(view, widthInCells) { StartIndex = desiredStart };
            _items.Add(item);

            PackToFit();
            LayoutAll();
            return view;
        }

        public bool RemoveShopCard(Widget_CardView view)
        {
            if (view == null)
                return false;

            bool removed = false;
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (_items[i].View == view)
                {
                    _items.RemoveAt(i);
                    removed = true;
                }
            }

            if (removed)
            {
                PackToFit();
                LayoutAll();
            }

            return removed;
        }

        public override bool CanAccept(DragPayload payload, PointerEventData eventData) => false;
        public override bool Accept(DragPayload payload, PointerEventData eventData) => false;

        protected override bool TryGetOccupiedCells(out bool[] occupied)
        {
            occupied = BuildOccupancy();
            return true;
        }

        private void OnCardClicked(Widget_CardView view)
        {
            if (view == null || view.Model == null)
                return;

            var purchaseSystem = Demo1Architecture.Instance.GetSystem<IShopPurchaseSystem>();
            if (purchaseSystem == null)
                return;

            var result = purchaseSystem.TryBuy(view.Model, out _);
            if (result != ShopPurchaseResult.Success)
                return;

            OnBuy?.Invoke(view.Model);

            if (_destroyOnBuy)
            {
                RemoveShopCard(view);
                UnityEngine.Object.Destroy(view.gameObject);
            }
        }

        private void LayoutAll()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item.View == null)
                    continue;

                var rt = item.View.RectTransform;
                if (rt == null)
                    continue;

                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);

                float width = item.WidthInCells * _cellWidth + (item.WidthInCells - 1) * _spacing;
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _cellHeight);

                float x = _padding.x + item.StartIndex * (_cellWidth + _spacing);
                rt.anchoredPosition = new Vector2(x, 0f);
            }
        }

        private void PackToFit()
        {
            if (_items.Count == 0)
                return;

            _items.RemoveAll(i => i.View == null);

            var ordered = _items.OrderBy(i => i.StartIndex).ToList();
            bool[] occ = new bool[_capacity];

            for (int i = 0; i < ordered.Count; i++)
            {
                var item = ordered[i];
                item.WidthInCells = Mathf.Clamp(item.WidthInCells, 1, 3);
                item.StartIndex = Mathf.Clamp(item.StartIndex, 0, Mathf.Max(0, _capacity - item.WidthInCells));

                int start = FindNearestFreeStart(occ, item.StartIndex, item.WidthInCells);
                item.StartIndex = start;
                MarkRegion(occ, start, item.WidthInCells, true);
            }
        }

        private int FindFirstFit(int widthInCells)
        {
            bool[] occ = BuildOccupancy();
            for (int start = 0; start <= _capacity - widthInCells; start++)
            {
                if (IsRegionFree(occ, start, widthInCells))
                    return start;
            }
            return -1;
        }

        private bool[] BuildOccupancy()
        {
            bool[] occ = new bool[_capacity];
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item == null || item.View == null)
                    continue;

                int start = Mathf.Clamp(item.StartIndex, 0, _capacity - item.WidthInCells);
                MarkRegion(occ, start, item.WidthInCells, true);
            }
            return occ;
        }

        private static int FindNearestFreeStart(bool[] occ, int preferredStart, int widthInCells)
        {
            int capacity = occ.Length;
            preferredStart = Mathf.Clamp(preferredStart, 0, capacity - widthInCells);

            int bestStart = -1;
            int bestCost = int.MaxValue;

            for (int start = 0; start <= capacity - widthInCells; start++)
            {
                if (!IsRegionFree(occ, start, widthInCells))
                    continue;

                int cost = Mathf.Abs(start - preferredStart);
                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestStart = start;
                }
            }

            return bestStart >= 0 ? bestStart : 0;
        }

        private static void MarkRegion(bool[] occ, int start, int width, bool value)
        {
            if (start < 0 || start + width > occ.Length)
                return;
            for (int i = start; i < start + width; i++)
                occ[i] = value;
        }

        private static bool IsRegionFree(bool[] occ, int start, int width)
        {
            if (start < 0 || start + width > occ.Length)
                return false;
            for (int i = start; i < start + width; i++)
            {
                if (occ[i])
                    return false;
            }
            return true;
        }

        private class ShopItem
        {
            public Widget_CardView View;
            public int WidthInCells;
            public int StartIndex;

            public ShopItem(Widget_CardView view, int widthInCells)
            {
                View = view;
                WidthInCells = widthInCells;
                StartIndex = 0;
            }
        }
    }
}
