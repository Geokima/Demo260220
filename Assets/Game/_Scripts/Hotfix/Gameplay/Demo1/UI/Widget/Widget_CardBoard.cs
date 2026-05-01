using System.Collections.Generic;
using System.Linq;
using Framework;
using Framework.Utils;
using Game.Gameplay.Demo1;
using Game.Gameplay.Demo1.System;
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Gameplay.Demo1.UI.Widget
{
    public class Widget_CardBoard : Widget_SlotZoneBase
    {
        [SerializeField] private Widget_CardView _cardPrefab;
        [SerializeField] private bool _draggable = true;

        private readonly List<CardItem> _items = new List<CardItem>();
        private BindableList<CardModel> _boundList;
        private bool _isSyncing;
        private IUnregister _onAddUnregister;
        private IUnregister _onRemoveUnregister;

        protected override int MinCapacity => 4;
        protected override int MaxCapacity => 10;

        public void BindTo(BindableList<CardModel> list)
        {
            Unbind();
            if (list == null)
                return;

            _boundList = list;
            _onAddUnregister = list.OnAdd.Register((index, card) =>
            {
                if (_isSyncing)
                    return;
                AddCard(card);
            });
            _onRemoveUnregister = list.OnRemove.Register((index, card) =>
            {
                if (_isSyncing)
                    return;
                RemoveCard(card);
            });
            Refresh();
        }

        public void Unbind()
        {
            _onAddUnregister?.Unregister();
            _onRemoveUnregister?.Unregister();
            _onAddUnregister = null;
            _onRemoveUnregister = null;
            _boundList = null;
        }

        public void Refresh()
        {
            ClearAllCards();
            if (_boundList == null)
                return;

            foreach (var card in _boundList)
            {
                if (card != null)
                    AddCard(card, card.StartIndex.Value);
            }
        }

        public void Init(int count)
        {
            SetCapacity(count);
        }

        public void SetCapacity(int capacity)
        {
            int targetCapacity = Mathf.Clamp(capacity, MinCapacity, MaxCapacity);
            if (_items.Count > 0 && targetCapacity < _capacity)
                targetCapacity = _capacity;

            if (targetCapacity == _capacity)
                return;

            int oldCapacity = _capacity;
            SetCapacityInternal(targetCapacity);

            int deltaCapacity = _capacity - oldCapacity;
            if (deltaCapacity > 0)
                ApplyCapacityShift(deltaCapacity);

            PackToFit();
            LayoutAll();
        }

        private void ApplyCapacityShift(int deltaCapacity)
        {
            if (deltaCapacity <= 0)
                return;

            if (_items.Count == 0)
                return;

            int shift = deltaCapacity / 2;
            if (shift == 0)
                return;

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item == null)
                    continue;

                item.StartIndex += shift;
            }
        }

        public void SetDraggable(bool draggable)
        {
            if (_draggable == draggable)
                return;

            _draggable = draggable;

            for (int i = 0; i < _items.Count; i++)
            {
                var view = _items[i].View;
                if (view == null)
                    continue;
                view.Draggable = _draggable;
            }

            if (!_draggable)
                Demo1Architecture.Instance.GetSystem<IDragSystem>().CancelCurrentDrag();
        }

        [Button]
        public void TestAddCard()
        {
            AddCard(new CardModel(new Demo1CardConfig { Name = _items.Count.ToString(), Size = 1, Rank = "Bronze", Type = "Active" }));
        }

        [Button]
        public void TestAddCardMid()
        {
            AddCard(new CardModel(new Demo1CardConfig { Name = _items.Count.ToString(), Size = 2, Rank = "Bronze", Type = "Active" }));
        }

        [Button]
        public void TestAddCardLarge()
        {
            AddCard(new CardModel(new Demo1CardConfig { Name = _items.Count.ToString(), Size = 3, Rank = "Bronze", Type = "Active" }));
        }

        public Widget_CardView AddCard(CardModel model, int? startIndex = null)
        {
            if (_cardPrefab == null)
                return null;

            PackToFit();

            var view = UnityEngine.Object.Instantiate(_cardPrefab, transform);
            view.Model = model;
            view.Draggable = _draggable;
            view.OwnerZone = this;

            var item = new CardItem(view, Mathf.Clamp(view.WidthInCells, 1, 3));

            int desiredStart = startIndex ?? FindFirstFit(item.WidthInCells, null);
            if (desiredStart < 0)
                desiredStart = Mathf.Clamp(_capacity - item.WidthInCells, 0, _capacity - item.WidthInCells);

            float unit = _cellWidth + _spacing;
            float cardWidth = item.WidthInCells * _cellWidth + (item.WidthInCells - 1) * _spacing;
            float dragCenterX = _padding.x + desiredStart * unit + cardWidth * 0.5f;

            if (!TryApplyOrderedInsert(item, dragCenterX, desiredStart, apply: true, out _, out _))
            {
                UnityEngine.Object.Destroy(view.gameObject);
                return null;
            }

            LayoutAll();
            return view;
        }

        public bool RemoveCard(Widget_CardView view)
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
                if (view.OwnerZone == this)
                    view.OwnerZone = null;
                LayoutAll();
            }

            return removed;
        }

        public bool RemoveCard(CardModel model)
        {
            if (model == null)
                return false;

            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (_items[i].View?.Model == model)
                {
                    var view = _items[i].View;
                    _items.RemoveAt(i);
                    if (view?.OwnerZone == this)
                        view.OwnerZone = null;
                    LayoutAll();
                    return true;
                }
            }
            return false;
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

        private void SyncBoundList()
        {
            if (_boundList == null)
                return;

            var orderedItems = _items
                .Where(i => i?.View?.Model != null)
                .OrderBy(i => i.StartIndex)
                .ToList();

            foreach (var item in orderedItems)
                item.View.Model.StartIndex.Value = item.StartIndex;

            _isSyncing = true;
            try
            {
                _boundList.Clear();
                for (int i = 0; i < orderedItems.Count; i++)
                    _boundList.Add(orderedItems[i].View.Model);
            }
            finally
            {
                _isSyncing = false;
            }
        }

        public override bool CanAccept(DragPayload payload, PointerEventData eventData)
        {
            if (!_draggable)
                return false;

            if (payload == null || payload.View == null)
                return false;

            if (payload.WidthInCells > _capacity)
                return false;

            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_rectTransform == null)
                return false;

            return RectTransformUtility.RectangleContainsScreenPoint(_rectTransform, eventData.position, eventData.pressEventCamera);
        }

        public override bool Accept(DragPayload payload, PointerEventData eventData)
        {
            if (!CanAccept(payload, eventData))
                return false;

            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            var view = payload.View;
            if (view == null)
                return false;

            if (payload.WidthInCells > _capacity)
                return false;

            int widthInCells = Mathf.Clamp(payload.WidthInCells, 1, 3);

            CardItem item = null;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].View == view)
                {
                    item = _items[i];
                    break;
                }
            }

            bool isNewItem = item == null;
            if (isNewItem)
                item = new CardItem(view, widthInCells);
            else
                item.WidthInCells = widthInCells;

            float dragCenterX = float.NaN;
            Vector2 centerScreenPos = eventData.position - payload.PointerOffsetFromCenterScreen;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rectTransform, centerScreenPos, eventData.pressEventCamera, out var localCenter))
            {
                dragCenterX = localCenter.x + _rectTransform.rect.width * _rectTransform.pivot.x;
            }

            if (float.IsNaN(dragCenterX))
                return false;

            int desiredStart = GetStartIndexFromCenterX(dragCenterX, widthInCells);
            if (!TryApplyOrderedInsert(item, dragCenterX, desiredStart, apply: true, out _, out _))
                return false;

            var oldBoard = view.OwnerZone as Widget_CardBoard;
            if (oldBoard != null && oldBoard != this)
                oldBoard.RemoveCard(view);

            view.RectTransform.SetParent(transform, false);
            view.Draggable = _draggable;
            view.OwnerZone = this;

            LayoutAll();
            SyncBoundList();
            if (oldBoard != null && oldBoard != this)
                oldBoard.SyncBoundList();
            return true;
        }

        private int GetStartIndexFromCenterX(float centerX, int widthInCells)
        {
            float unit = _cellWidth + _spacing;
            float cardWidth = widthInCells * _cellWidth + (widthInCells - 1) * _spacing;
            int desiredStart = Mathf.RoundToInt((centerX - _padding.x - cardWidth * 0.5f) / unit);
            return Mathf.Clamp(desiredStart, 0, _capacity - widthInCells);
        }

        private float GetCenterX(CardItem item)
        {
            float unit = _cellWidth + _spacing;
            float itemWidth = item.WidthInCells * _cellWidth + (item.WidthInCells - 1) * _spacing;
            float itemStartX = _padding.x + item.StartIndex * unit;
            return itemStartX + itemWidth * 0.5f;
        }

        private bool TryApplyOrderedInsert(
            CardItem dragging,
            float dragCenterX,
            int desiredStart,
            bool apply,
            out List<CardItem> ordered,
            out Dictionary<CardItem, int> resultStarts)
        {
            ordered = _items
                .Where(i => i != null && i.View != null)
                .OrderBy(i => i.StartIndex)
                .ToList();

            if (dragging != null && dragging.View != null)
                ordered.RemoveAll(i => i.View == dragging.View);

            int insertIndex = ordered.Count;
            for (int i = 0; i < ordered.Count; i++)
            {
                if (GetCenterX(ordered[i]) > dragCenterX)
                {
                    insertIndex = i;
                    break;
                }
            }

            ordered.Insert(insertIndex, dragging);

            int count = ordered.Count;
            resultStarts = new Dictionary<CardItem, int>(count);

            var originalStarts = new int[count];
            for (int i = 0; i < count; i++)
            {
                int w = Mathf.Clamp(ordered[i].WidthInCells, 1, 3);
                originalStarts[i] = Mathf.Clamp(ordered[i].StartIndex, 0, _capacity - w);
            }

            int dragWidth = Mathf.Clamp(dragging.WidthInCells, 1, 3);
            desiredStart = Mathf.Clamp(desiredStart, 0, _capacity - dragWidth);

            var orderedLocal = ordered;
            bool SimulateAt(int dragStart, out int[] starts)
            {
                starts = new int[count];
                for (int i = 0; i < count; i++)
                    starts[i] = originalStarts[i];

                starts[insertIndex] = dragStart;

                int prevEnd = dragStart + dragWidth;
                for (int i = insertIndex + 1; i < count; i++)
                {
                    int w = Mathf.Clamp(orderedLocal[i].WidthInCells, 1, 3);
                    int s = Mathf.Max(originalStarts[i], prevEnd);
                    if (s > _capacity - w)
                        return false;
                    starts[i] = s;
                    prevEnd = s + w;
                }

                int nextStart = dragStart;
                for (int i = insertIndex - 1; i >= 0; i--)
                {
                    int w = Mathf.Clamp(orderedLocal[i].WidthInCells, 1, 3);
                    int s = Mathf.Min(originalStarts[i], nextStart - w);
                    if (s < 0)
                        return false;
                    starts[i] = s;
                    nextStart = s;
                }

                return true;
            }

            int[] solvedStarts = null;
            bool solved = false;
            int maxStart = _capacity - dragWidth;
            int maxOffset = Mathf.Max(desiredStart, maxStart - desiredStart);
            for (int offset = 0; offset <= maxOffset; offset++)
            {
                int left = desiredStart - offset;
                if (left >= 0)
                {
                    if (SimulateAt(left, out solvedStarts))
                    {
                        solved = true;
                        break;
                    }
                }

                if (offset == 0)
                    continue;

                int right = desiredStart + offset;
                if (right <= maxStart)
                {
                    if (SimulateAt(right, out solvedStarts))
                    {
                        solved = true;
                        break;
                    }
                }
            }

            if (!solved)
                return false;

            for (int i = 0; i < count; i++)
                resultStarts[ordered[i]] = solvedStarts[i];

            if (apply)
            {
                for (int i = 0; i < ordered.Count; i++)
                    ordered[i].StartIndex = resultStarts[ordered[i]];

                _items.Clear();
                _items.AddRange(ordered);
            }

            return true;
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

        private int FindFirstFit(int widthInCells, CardItem exclude)
        {
            bool[] occ = BuildOccupancy(exclude);
            for (int start = 0; start <= _capacity - widthInCells; start++)
            {
                if (IsRegionFree(occ, start, widthInCells))
                    return start;
            }
            return -1;
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

        private bool[] BuildOccupancy(CardItem exclude)
        {
            bool[] occ = new bool[_capacity];
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item == null || item.View == null || item == exclude)
                    continue;

                int start = Mathf.Clamp(item.StartIndex, 0, _capacity - item.WidthInCells);
                MarkRegion(occ, start, item.WidthInCells, true);
            }
            return occ;
        }

        protected override bool TryGetOccupiedCells(out bool[] occupied)
        {
            occupied = BuildOccupancy(null);
            return true;
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            if (!Application.isPlaying)
                return;

            var rt = GetComponent<RectTransform>();
            if (rt == null)
                return;

            var mgr = Demo1Architecture.Instance.GetSystem<IDragSystem>();
            if (!mgr.IsDragging)
                return;

            var payload = mgr.CurrentPayload;
            if (payload == null)
                return;

            Vector2 pointerScreen = mgr.CurrentPointerScreenPos;
            var cam = mgr.CurrentEventCamera;

            if (!RectTransformUtility.RectangleContainsScreenPoint(rt, pointerScreen, cam))
                return;

            Vector2 centerScreenPos = pointerScreen - payload.PointerOffsetFromCenterScreen;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, centerScreenPos, cam, out var localCenter))
                return;

            float originX = -rt.rect.width * rt.pivot.x;
            float centerY = -rt.rect.height * rt.pivot.y + _padding.y + _cellHeight * 0.5f;
            float centerXPivot = localCenter.x;
            float centerXLeft = centerXPivot - originX;

            int widthInCells = Mathf.Clamp(payload.WidthInCells, 1, 3);
            int desiredStart = GetStartIndexFromCenterX(centerXLeft, widthInCells);

            var oldMatrix = Gizmos.matrix;
            Gizmos.matrix = rt.localToWorldMatrix;

            float unit = _cellWidth + _spacing;
            float dragWidth = widthInCells * _cellWidth + (widthInCells - 1) * _spacing;

            int desiredEnd = desiredStart + widthInCells - 1;

            float desiredStartX = originX + _padding.x + desiredStart * unit;
            float desiredEndX = desiredStartX + dragWidth;

            Gizmos.color = new Color(0.25f, 1f, 0.35f, 0.9f);
            Gizmos.DrawLine(new Vector3(centerXPivot, centerY - _cellHeight * 0.6f, 0f), new Vector3(centerXPivot, centerY + _cellHeight * 0.6f, 0f));

            var orderedCurrent = _items
                .Where(i => i != null && i.View != null && i.View != payload.View)
                .OrderBy(i => i.StartIndex)
                .ToList();

            int insertIndex = orderedCurrent.Count;
            for (int i = 0; i < orderedCurrent.Count; i++)
            {
                if (GetCenterX(orderedCurrent[i]) > centerXLeft)
                {
                    insertIndex = i;
                    break;
                }
            }

            for (int i = insertIndex; i < orderedCurrent.Count; i++)
                DrawItemFill(orderedCurrent[i].StartIndex, orderedCurrent[i].WidthInCells, new Color(1f, 0.8f, 0.2f, 0.4f));

            DrawItemFill(desiredStart, widthInCells, new Color(0.3f, 0.8f, 1f, 0.5f));

            for (int i = 0; i < insertIndex; i++)
                DrawItemFill(orderedCurrent[i].StartIndex, orderedCurrent[i].WidthInCells, new Color(1f, 0.8f, 0.2f, 0.4f));

            Gizmos.matrix = oldMatrix;

            void DrawItemFill(int startIndex, int itemWidthInCells, Color fill)
            {
                itemWidthInCells = Mathf.Clamp(itemWidthInCells, 1, 3);
                startIndex = Mathf.Clamp(startIndex, 0, _capacity - itemWidthInCells);

                float w = itemWidthInCells * _cellWidth + (itemWidthInCells - 1) * _spacing;
                float x0 = originX + _padding.x + startIndex * unit;
                float cx = x0 + w * 0.5f;

                Gizmos.color = fill;
                Gizmos.DrawCube(new Vector3(cx, centerY, 0f), new Vector3(w, _cellHeight, 0.01f));
            }
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

        private class CardItem
        {
            public Widget_CardView View;
            public int WidthInCells;
            public int StartIndex;

            public CardItem(Widget_CardView view, int widthInCells)
            {
                View = view;
                WidthInCells = widthInCells;
                StartIndex = 0;
            }
        }
    }
}
