using System.Collections.Generic;
using System.Linq;
using Framework.Utils;
using Game.Gameplay.Demo1;
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Gameplay.Demo1.UI
{
    public class UI_CardBoard : UI_SlotZoneBase
    {
        [SerializeField] private UI_CardView _cardPrefab;

        [SerializeField] private bool _draggable = true;

        private readonly List<CardItem> _items = new List<CardItem>();

        protected override int MinCapacity => 4;
        protected override int MaxCapacity => 10;

        public void Init(int count)
        {
            SetCapacity(count);
        }

        public void SetCapacity(int capacity)
        {
            SetCapacityInternal(capacity);

            PackToFit();
            LayoutAll();
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
                UIDragManager.Instance.CancelCurrentDrag();
        }
        
        [Button]
        public void TestAddCard()
        {
            AddCard(new CardModel(new CardData{Name = _items.Count.ToString(), Size = 1 }));
        }

        [Button]
        public void TestAddCardMid()
        {
            AddCard(new CardModel(new CardData{Name = _items.Count.ToString(), Size = 2 }));
        }

        [Button]
        public void TestAddCardLarge()
        {
            AddCard(new CardModel(new CardData{Name = _items.Count.ToString(), Size = 3 }));
        }

        public UI_CardView AddCard(CardModel model, int? startIndex = null)
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

            if (!TryApplyOrderedInsert(item, dragCenterX, desiredStart, movingWithinSameBoard: false, apply: true, out _, out _))
            {
                UnityEngine.Object.Destroy(view.gameObject);
                return null;
            }

            LayoutAll();
            return view;
        }

        public bool RemoveCard(UI_CardView view)
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

        public override bool CanAccept(UIDragPayload payload, PointerEventData eventData)
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

        public override void Preview(UIDragPayload payload, PointerEventData eventData)
        {
        }

        public override void CancelPreview(UIDragPayload payload)
        {
        }

        public override bool Accept(UIDragPayload payload, PointerEventData eventData)
        {
            if (!CanAccept(payload, eventData))
                return false;

            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            var view = payload.View;
            if (view == null)
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

            bool movingWithinSameBoard = !isNewItem && view.OwnerZone == this;
            int desiredStart = GetStartIndexFromCenterX(dragCenterX, widthInCells);
            if (!TryApplyOrderedInsert(item, dragCenterX, desiredStart, movingWithinSameBoard, apply: true, out _, out _))
                return false;

            if (view.OwnerZone is UI_CardBoard oldBoard && oldBoard != this)
                oldBoard.RemoveCard(view);

            view.RectTransform.SetParent(transform, false);
            view.Draggable = _draggable;
            view.OwnerZone = this;

            LayoutAll();
            return true;
        }

        private int GetStartIndexFromCenterX(float centerX, int widthInCells)
        {
            float unit = _cellWidth + _spacing;
            float cardWidth = widthInCells * _cellWidth + (widthInCells - 1) * _spacing;
            int desiredStart = Mathf.RoundToInt((centerX - _padding.x - cardWidth * 0.5f) / unit);
            return Mathf.Clamp(desiredStart, 0, _capacity - widthInCells);
        }

        private int GetStartIndexFromLeftX(float leftX, int widthInCells)
        {
            float unit = _cellWidth + _spacing;
            int desiredStart = Mathf.RoundToInt((leftX - _padding.x) / unit);
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
            bool movingWithinSameBoard,
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
            for (int dragStart = desiredStart; dragStart >= 0; dragStart--)
            {
                if (SimulateAt(dragStart, out solvedStarts))
                {
                    solved = true;
                    break;
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

        private bool TryGetNearestOverlappingItem(int start, int widthInCells, CardItem exclude, float dragCenterX, out CardItem blocker, out float blockerCenterX)
        {
            blocker = null;
            blockerCenterX = 0f;

            if (start < 0 || start + widthInCells > _capacity)
                return false;

            float unit = _cellWidth + _spacing;

            float bestDist = float.MaxValue;
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item == null || item.View == null || item == exclude)
                    continue;

                int a0 = start;
                int a1 = start + widthInCells - 1;
                int b0 = item.StartIndex;
                int b1 = item.StartIndex + item.WidthInCells - 1;
                bool overlaps = a0 <= b1 && a1 >= b0;
                if (!overlaps)
                    continue;

                float itemWidth = item.WidthInCells * _cellWidth + (item.WidthInCells - 1) * _spacing;
                float itemStartX = _padding.x + item.StartIndex * unit;
                float centerX = itemStartX + itemWidth * 0.5f;
                float dist = Mathf.Abs(centerX - dragCenterX);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    blocker = item;
                    blockerCenterX = centerX;
                }
            }

            return blocker != null;
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

        private bool TryApplyInsertLayout(CardItem dragging, int desiredStart, out Dictionary<CardItem, int> resultStarts)
        {
            if (TryInsertWithDirection(dragging, desiredStart, 1, out resultStarts))
            {
                ApplyResult(resultStarts);
                return true;
            }

            if (TryInsertWithDirection(dragging, desiredStart, -1, out resultStarts))
            {
                ApplyResult(resultStarts);
                return true;
            }

            return false;
        }

        private bool TryInsertWithDirection(CardItem dragging, int desiredStart, int direction, out Dictionary<CardItem, int> resultStarts)
        {
            resultStarts = new Dictionary<CardItem, int>();

            if (_capacity <= 0)
                return false;

            desiredStart = Mathf.Clamp(desiredStart, 0, _capacity - dragging.WidthInCells);

            bool[] occ = new bool[_capacity];
            CardItem[] owner = new CardItem[_capacity];

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item == null || item.View == null || item == dragging)
                    continue;

                int start = Mathf.Clamp(item.StartIndex, 0, _capacity - item.WidthInCells);
                if (!CanMarkRegion(occ, start, item.WidthInCells))
                    continue;

                MarkRegion(occ, start, item.WidthInCells, true);
                for (int c = start; c < start + item.WidthInCells; c++)
                    owner[c] = item;

                resultStarts[item] = start;
            }

            int safety = 64;
            while (!IsRegionFree(occ, desiredStart, dragging.WidthInCells))
            {
                if (safety-- <= 0)
                    return false;

                int blockingCell = FindBlockingCell(occ, desiredStart, dragging.WidthInCells, direction);
                if (blockingCell < 0)
                    return false;

                var blocker = owner[blockingCell];
                if (blocker == null)
                    return false;

                int currentStart = resultStarts.TryGetValue(blocker, out var s) ? s : blocker.StartIndex;
                MarkRegion(occ, currentStart, blocker.WidthInCells, false);
                for (int c = currentStart; c < currentStart + blocker.WidthInCells; c++)
                    owner[c] = null;

                int nextStart = FindNextFreeStart(occ, desiredStart, dragging.WidthInCells, currentStart, blocker.WidthInCells, direction);
                if (nextStart < 0)
                    return false;

                MarkRegion(occ, nextStart, blocker.WidthInCells, true);
                for (int c = nextStart; c < nextStart + blocker.WidthInCells; c++)
                    owner[c] = blocker;

                resultStarts[blocker] = nextStart;
            }

            resultStarts[dragging] = desiredStart;
            return true;
        }

        private int FindBlockingCell(bool[] occ, int start, int width, int direction)
        {
            if (direction >= 0)
            {
                for (int i = start + width - 1; i >= start; i--)
                {
                    if (occ[i])
                        return i;
                }
                return -1;
            }

            for (int i = start; i < start + width; i++)
            {
                if (occ[i])
                    return i;
            }
            return -1;
        }

        private bool TryApplyStableMove(CardItem dragging, int newStart)
        {
            if (dragging == null || dragging.View == null)
                return false;

            int width = Mathf.Clamp(dragging.WidthInCells, 1, 3);
            int oldStart = Mathf.Clamp(dragging.StartIndex, 0, _capacity - width);
            newStart = Mathf.Clamp(newStart, 0, _capacity - width);

            if (newStart == oldStart)
                return true;

            var resultStarts = new Dictionary<CardItem, int>();
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item == null || item.View == null)
                    continue;
                resultStarts[item] = Mathf.Clamp(item.StartIndex, 0, _capacity - Mathf.Clamp(item.WidthInCells, 1, 3));
            }

            int oldEnd = oldStart + width - 1;
            int newEnd = newStart + width - 1;

            int affectedStart;
            int affectedEnd;
            int delta;
            if (newStart < oldStart)
            {
                affectedStart = newStart;
                affectedEnd = oldStart - 1;
                delta = width;
            }
            else
            {
                affectedStart = oldEnd + 1;
                affectedEnd = newEnd;
                delta = -width;
            }

            if (affectedStart <= affectedEnd)
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    var item = _items[i];
                    if (item == null || item.View == null || item == dragging)
                        continue;

                    int itemWidth = Mathf.Clamp(item.WidthInCells, 1, 3);
                    int itemStart = Mathf.Clamp(item.StartIndex, 0, _capacity - itemWidth);
                    int itemEnd = itemStart + itemWidth - 1;
                    bool intersects = itemStart <= affectedEnd && itemEnd >= affectedStart;
                    if (intersects)
                        resultStarts[item] = itemStart + delta;
                }
            }

            resultStarts[dragging] = newStart;

            bool[] occ = new bool[_capacity];
            foreach (var kv in resultStarts)
            {
                var item = kv.Key;
                int itemWidth = Mathf.Clamp(item.WidthInCells, 1, 3);
                int start = kv.Value;
                if (start < 0 || start > _capacity - itemWidth)
                    return false;
                if (!IsRegionFree(occ, start, itemWidth))
                    return false;
                MarkRegion(occ, start, itemWidth, true);
            }

            ApplyResult(resultStarts);
            return true;
        }

        private int FindNextFreeStart(bool[] occ, int forbiddenStart, int forbiddenWidth, int fromStart, int width, int direction)
        {
            if (direction >= 0)
            {
                for (int start = fromStart + 1; start <= _capacity - width; start++)
                {
                    if (IsRegionFreeWithForbidden(occ, start, width, forbiddenStart, forbiddenWidth))
                        return start;
                }
                return -1;
            }

            for (int start = fromStart - 1; start >= 0; start--)
            {
                if (start > _capacity - width)
                    continue;

                if (IsRegionFreeWithForbidden(occ, start, width, forbiddenStart, forbiddenWidth))
                    return start;
            }
            return -1;
        }

        private void ApplyResult(Dictionary<CardItem, int> resultStarts)
        {
            foreach (var kv in resultStarts)
                kv.Key.StartIndex = kv.Value;
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

            var mgr = UnityEngine.Object.FindObjectOfType<UIDragManager>();
            if (mgr == null || !mgr.IsDragging)
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

            float centerX = localCenter.x + rt.rect.width * rt.pivot.x;

            int widthInCells = Mathf.Clamp(payload.WidthInCells, 1, 3);
            int desiredStart = GetStartIndexFromCenterX(centerX, widthInCells);

            CardItem draggingItem = null;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].View == payload.View)
                {
                    draggingItem = _items[i];
                    break;
                }
            }

            bool movingWithinSameBoard = draggingItem != null && payload.View.OwnerZone == this;
            if (draggingItem == null)
                draggingItem = new CardItem(payload.View, widthInCells);
            else
                draggingItem.WidthInCells = widthInCells;

            if (!TryApplyOrderedInsert(draggingItem, centerX, desiredStart, movingWithinSameBoard, apply: false, out var ordered, out var starts))
                return;

            if (!starts.TryGetValue(draggingItem, out int predictedStart))
                return;

            var oldMatrix = Gizmos.matrix;
            Gizmos.matrix = rt.localToWorldMatrix;

            Gizmos.color = new Color(0.25f, 1f, 0.35f, 0.9f);
            Gizmos.DrawLine(new Vector3(centerX, -_cellHeight * 0.6f, 0f), new Vector3(centerX, _cellHeight * 0.6f, 0f));

            float unit = _cellWidth + _spacing;
            float dragWidth = widthInCells * _cellWidth + (widthInCells - 1) * _spacing;

            int desiredEnd = desiredStart + widthInCells - 1;
            int predictedEnd = predictedStart + widthInCells - 1;

            float desiredStartX = _padding.x + desiredStart * unit;
            float desiredEndX = desiredStartX + dragWidth;
            float predictedStartX = _padding.x + predictedStart * unit;
            float predictedEndX = predictedStartX + dragWidth;

            Gizmos.color = new Color(0.7f, 0.7f, 0.7f, 0.85f);
            Gizmos.DrawWireCube(new Vector3(desiredStartX + dragWidth * 0.5f, 0f, 0f), new Vector3(dragWidth, _cellHeight, 0.01f));
            Gizmos.color = new Color(0.7f, 0.7f, 0.7f, 0.6f);
            Gizmos.DrawLine(new Vector3(desiredStartX, -_cellHeight * 0.55f, 0f), new Vector3(desiredStartX, _cellHeight * 0.55f, 0f));
            Gizmos.DrawLine(new Vector3(desiredEndX, -_cellHeight * 0.55f, 0f), new Vector3(desiredEndX, _cellHeight * 0.55f, 0f));

            Gizmos.color = new Color(0.95f, 0.75f, 0.2f, 0.18f);
            Gizmos.DrawCube(new Vector3(predictedStartX + dragWidth * 0.5f, 0f, 0f), new Vector3(dragWidth, _cellHeight, 0.01f));
            Gizmos.color = new Color(0.95f, 0.75f, 0.2f, 0.85f);
            Gizmos.DrawWireCube(new Vector3(predictedStartX + dragWidth * 0.5f, 0f, 0f), new Vector3(dragWidth, _cellHeight, 0.01f));
            Gizmos.color = new Color(0.95f, 0.75f, 0.2f, 0.6f);
            Gizmos.DrawLine(new Vector3(predictedStartX, -_cellHeight * 0.55f, 0f), new Vector3(predictedStartX, _cellHeight * 0.55f, 0f));
            Gizmos.DrawLine(new Vector3(predictedEndX, -_cellHeight * 0.55f, 0f), new Vector3(predictedEndX, _cellHeight * 0.55f, 0f));

            for (int c = desiredStart; c <= desiredEnd; c++)
            {
                float cellX = _padding.x + c * unit + _cellWidth * 0.5f;
                Gizmos.color = new Color(0.75f, 0.75f, 0.75f, 0.07f);
                Gizmos.DrawCube(new Vector3(cellX, 0f, 0f), new Vector3(_cellWidth, _cellHeight, 0.01f));
            }

            for (int c = predictedStart; c <= predictedEnd; c++)
            {
                float cellX = _padding.x + c * unit + _cellWidth * 0.5f;
                Gizmos.color = new Color(0.95f, 0.75f, 0.2f, 0.10f);
                Gizmos.DrawCube(new Vector3(cellX, 0f, 0f), new Vector3(_cellWidth, _cellHeight, 0.01f));
            }

            Gizmos.matrix = oldMatrix;

#if UNITY_EDITOR
            var labelWorld = rt.TransformPoint(new Vector3(_padding.x, _cellHeight * 0.55f, 0f));
            Handles.Label(labelWorld, $"w:{widthInCells} desired:{desiredStart}-{desiredEnd} predicted:{predictedStart}-{predictedEnd}");
#endif
        }

        private static bool CanMarkRegion(bool[] occ, int start, int width)
        {
            if (start < 0 || start + width > occ.Length)
                return false;
            return true;
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

        private static bool IsRegionFreeWithForbidden(bool[] occ, int start, int width, int forbiddenStart, int forbiddenWidth)
        {
            if (!IsRegionFree(occ, start, width))
                return false;

            int end = start + width - 1;
            int forbiddenEnd = forbiddenStart + forbiddenWidth - 1;
            bool overlapsForbidden = start <= forbiddenEnd && end >= forbiddenStart;
            return !overlapsForbidden;
        }

        private class CardItem
        {
            public UI_CardView View;
            public int WidthInCells;
            public int StartIndex;

            public CardItem(UI_CardView view, int widthInCells)
            {
                View = view;
                WidthInCells = widthInCells;
                StartIndex = 0;
            }
        }
    }
}
