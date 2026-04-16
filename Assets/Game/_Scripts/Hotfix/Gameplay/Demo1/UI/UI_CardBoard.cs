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
                UIDragManager.Instance.CancelCurrentDrag();
        }
        [Button]
        public void UpdateCapcityToMax()
        {
            SetCapacity(MaxCapacity);
        }

        [Button]
        public void TestAddCard()
        {
            AddCard(new CardModel(new CardData { Name = _items.Count.ToString(), Size = 1 }));
        }

        [Button]
        public void TestAddCardMid()
        {
            AddCard(new CardModel(new CardData { Name = _items.Count.ToString(), Size = 2 }));
        }

        [Button]
        public void TestAddCardLarge()
        {
            AddCard(new CardModel(new CardData { Name = _items.Count.ToString(), Size = 3 }));
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

            if (!TryApplyOrderedInsert(item, dragCenterX, desiredStart, apply: true, out _, out _))
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

            int desiredStart = GetStartIndexFromCenterX(dragCenterX, widthInCells);
            if (!TryApplyOrderedInsert(item, dragCenterX, desiredStart, apply: true, out _, out _))
                return false;

            if (view.OwnerZone is UI_CardBoard oldBoard && oldBoard != this)
                oldBoard.RemoveCard(view);

            view.RectTransform.SetParent(transform, false);
            view.Draggable = _draggable;
            view.OwnerZone = this;

            LayoutAll();
            return true;
        }

        // 将“拖拽卡牌中心点的 local X”换算成“左侧起始格子索引”，并做边界夹紧
        private int GetStartIndexFromCenterX(float centerX, int widthInCells)
        {
            float unit = _cellWidth + _spacing;
            float cardWidth = widthInCells * _cellWidth + (widthInCells - 1) * _spacing;
            int desiredStart = Mathf.RoundToInt((centerX - _padding.x - cardWidth * 0.5f) / unit);
            return Mathf.Clamp(desiredStart, 0, _capacity - widthInCells);
        }

        // 根据 item 的 StartIndex / WidthInCells 计算它在面板坐标系中的中心点 X
        private float GetCenterX(CardItem item)
        {
            float unit = _cellWidth + _spacing;
            float itemWidth = item.WidthInCells * _cellWidth + (item.WidthInCells - 1) * _spacing;
            float itemStartX = _padding.x + item.StartIndex * unit;
            return itemStartX + itemWidth * 0.5f;
        }

        // 按“拖拽中心点 X”决定插入位置，然后尝试求出所有卡牌在不重叠前提下的 StartIndex；apply=true 时会把结果写回 _items
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

        // 按 _items 的 StartIndex / WidthInCells，把所有 UI 卡牌的 RectTransform 尺寸与位置刷新到正确格子
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

        // 从左到右寻找第一个能放下 widthInCells 的空位（exclude 参与占用排除）
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

        // 把现有卡牌尽量“挤紧”到可放下的区域里：按 StartIndex 排序后，逐个找离原位置最近的空位并占用
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

        // 在占用数组 occ 中，寻找一个能容纳 widthInCells 的空位，且尽量靠近 preferredStart（以距离作为代价）
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

        // 根据当前 _items 生成占用表：occ[i]=true 表示该格已被占用（exclude 不计入占用）
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

            // 1) Drag center line (always)
            Gizmos.color = new Color(0.25f, 1f, 0.35f, 0.9f);
            Gizmos.DrawLine(new Vector3(centerXPivot, centerY - _cellHeight * 0.6f, 0f), new Vector3(centerXPivot, centerY + _cellHeight * 0.6f, 0f));

            // 2) All cards left/right of insertion point (always)
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

            var leftFill = new Color(0.05f, 0.7f, 1f, 0.16f);
            var rightFill = new Color(1f, 0.15f, 0.85f, 0.16f);

            for (int i = 0; i < orderedCurrent.Count; i++)
            {
                var item = orderedCurrent[i];
                if (item == null)
                    continue;

                var fill = i < insertIndex ? leftFill : rightFill;
                DrawItemFill(item.StartIndex, item.WidthInCells, fill);
            }

            // Desired position outline (always)
            Gizmos.color = new Color(0.7f, 0.7f, 0.7f, 0.85f);
            Gizmos.DrawWireCube(new Vector3(desiredStartX + dragWidth * 0.5f, centerY, 0f), new Vector3(dragWidth, _cellHeight, 0.01f));
            Gizmos.color = new Color(0.7f, 0.7f, 0.7f, 0.6f);
            Gizmos.DrawLine(new Vector3(desiredStartX, centerY - _cellHeight * 0.55f, 0f), new Vector3(desiredStartX, centerY + _cellHeight * 0.55f, 0f));
            Gizmos.DrawLine(new Vector3(desiredEndX, centerY - _cellHeight * 0.55f, 0f), new Vector3(desiredEndX, centerY + _cellHeight * 0.55f, 0f));

            // 3) Predicted position (only when solvable)
            bool hasPrediction = false;
            int predictedStart = -1;
            int predictedEnd = -1;
            {
                CardItem draggingItem = null;
                for (int i = 0; i < _items.Count; i++)
                {
                    if (_items[i].View == payload.View)
                    {
                        draggingItem = _items[i];
                        break;
                    }
                }

                if (draggingItem == null)
                    draggingItem = new CardItem(payload.View, widthInCells);
                else
                    draggingItem.WidthInCells = widthInCells;

                if (TryApplyOrderedInsert(draggingItem, centerXLeft, desiredStart, apply: false, out _, out var starts)
                    && starts.TryGetValue(draggingItem, out predictedStart))
                {
                    hasPrediction = true;
                    predictedEnd = predictedStart + widthInCells - 1;

                    float predictedStartX = originX + _padding.x + predictedStart * unit;
                    float predictedEndX = predictedStartX + dragWidth;

                    Gizmos.color = new Color(0.95f, 0.75f, 0.2f, 0.18f);
                    Gizmos.DrawCube(new Vector3(predictedStartX + dragWidth * 0.5f, centerY, 0f), new Vector3(dragWidth, _cellHeight, 0.01f));
                    Gizmos.color = new Color(0.95f, 0.75f, 0.2f, 0.85f);
                    Gizmos.DrawWireCube(new Vector3(predictedStartX + dragWidth * 0.5f, centerY, 0f), new Vector3(dragWidth, _cellHeight, 0.01f));
                    Gizmos.color = new Color(0.95f, 0.75f, 0.2f, 0.6f);
                    Gizmos.DrawLine(new Vector3(predictedStartX, centerY - _cellHeight * 0.55f, 0f), new Vector3(predictedStartX, centerY + _cellHeight * 0.55f, 0f));
                    Gizmos.DrawLine(new Vector3(predictedEndX, centerY - _cellHeight * 0.55f, 0f), new Vector3(predictedEndX, centerY + _cellHeight * 0.55f, 0f));
                }
            }

            for (int c = desiredStart; c <= desiredEnd; c++)
            {
                float cellX = originX + _padding.x + c * unit + _cellWidth * 0.5f;
                Gizmos.color = new Color(0.75f, 0.75f, 0.75f, 0.07f);
                Gizmos.DrawCube(new Vector3(cellX, centerY, 0f), new Vector3(_cellWidth, _cellHeight, 0.01f));
            }

            if (hasPrediction)
            {
                for (int c = predictedStart; c <= predictedEnd; c++)
                {
                    float cellX = originX + _padding.x + c * unit + _cellWidth * 0.5f;
                    Gizmos.color = new Color(0.95f, 0.75f, 0.2f, 0.10f);
                    Gizmos.DrawCube(new Vector3(cellX, centerY, 0f), new Vector3(_cellWidth, _cellHeight, 0.01f));
                }
            }
            else
            {
                // No solution marker at desired region
                Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.9f);
                Gizmos.DrawLine(new Vector3(desiredStartX, centerY - _cellHeight * 0.45f, 0f), new Vector3(desiredEndX, centerY + _cellHeight * 0.45f, 0f));
                Gizmos.DrawLine(new Vector3(desiredStartX, centerY + _cellHeight * 0.45f, 0f), new Vector3(desiredEndX, centerY - _cellHeight * 0.45f, 0f));
            }

            Gizmos.matrix = oldMatrix;

#if UNITY_EDITOR
            var labelWorld = rt.TransformPoint(new Vector3(originX + _padding.x, centerY + _cellHeight * 0.55f, 0f));
            var predictedText = hasPrediction ? $"{predictedStart}-{predictedEnd}" : "NONE";
            Handles.Label(labelWorld, $"w:{widthInCells} desired:{desiredStart}-{desiredEnd} insertIndex:{insertIndex}/{orderedCurrent.Count} predicted:{predictedText}");
#endif
        }

        // 判断给定区间 [start, start+width) 是否在 occ 数组范围内（工具函数，避免越界）
        private static bool CanMarkRegion(bool[] occ, int start, int width)
        {
            if (start < 0 || start + width > occ.Length)
                return false;
            return true;
        }

        // 将 occ 在区间 [start, start+width) 的占用标记为 value（越界则直接忽略）
        private static void MarkRegion(bool[] occ, int start, int width, bool value)
        {
            if (start < 0 || start + width > occ.Length)
                return;
            for (int i = start; i < start + width; i++)
                occ[i] = value;
        }

        // 判断 occ 在区间 [start, start+width) 是否全为空（未被占用）
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

        // 在 IsRegionFree 的基础上，再额外排除与 forbidden 区间的重叠（用于拖拽预测/禁区检测）
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
