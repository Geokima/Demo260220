using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using Framework.Utils;

namespace Game.UI
{
    /// <summary>
    /// 手牌容器。交互状态机：
    ///
    ///  [无选中]
    ///    PointerDown on card → 选中 + 置顶
    ///
    ///  [选中 · 按住]
    ///    拖动超阈值 → 跟随模式（拖拽）
    ///    松开（未拖）→ 持久选中
    ///
    ///  [持久选中]
    ///    任意处 PointerDown → 跟随模式（持久跟随）
    ///
    ///  [跟随模式]（拖拽 和 持久跟随 共用）
    ///    鼠标松开 → 取消选中，飞回
    /// </summary>
    public class HandLayout : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────
        [Header("布局")]
        public float cardSpacing   = 100f;
        public float maxTotalWidth = 500f;

        [Header("Hover / 选中效果")]
        public float hoverScale     = 1.35f;
        public float pushAmount     = 50f;
        public int   pushRadius     = 3;
        public float maxTiltDegrees = -6f;
        public float arcDropY       = 40f;
        public float hoverRiseY     = 35f;
        [Tooltip("认定为 '拖拽' 的鼠标位移阈值（屏幕像素）")]
        public float dragThreshold  = 10f;

        [Header("测试发牌")]
        public float   cardWidth      = 100f;
        public float   cardHeight     = 150f;
        public Vector2 dealFromCorner = new Vector2(-850f, -480f);

        // ── 内部状态 ──────────────────────────────────────────────────────
        private readonly List<HandCard> _cards        = new();
        private int                     _hoveredIndex = -1;

        /// <summary>已被 PointerDown 选中的牌（置顶）</summary>
        private HandCard _selectedCard;
        /// <summary>正在跟随鼠标的牌</summary>
        private HandCard _followingCard;
        /// <summary>本次按下时鼠标的位置（用于拖拽阈值检测）</summary>
        private Vector2  _holdStartScreenPos;
        /// <summary>当前是否处于按住状态（PointerDown 后还未松开）</summary>
        private bool     _isHolding;

        private Camera   _eventCamera;
        private int      _testCardCount;

        /// <summary>当前操作中的牌（跟随 > 持久选中 > 悬停）</summary>
        public HandCard ActiveCard =>
            _followingCard ?? _selectedCard
            ?? (_hoveredIndex >= 0 ? _cards[_hoveredIndex] : null);

        /// <summary>跟随结束（松开鼠标）时触发。(card, Canvas本地松开坐标)</summary>
        public Action<HandCard, Vector2> OnFollowEnd;

        // ── 生命周期 ──────────────────────────────────────────────────────
        private void Awake()
        {
            var canvas   = GetComponentInParent<Canvas>();
            _eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera : null;
        }

        private void Update()
        {
            RefreshLayout();
            UpdateFollowStateMachine();
        }

        // ── 子物体管理 ────────────────────────────────────────────────────
        public void AddCard(HandCard card)
        {
            if (card.transform.parent != transform)
                card.transform.SetParent(transform, true);
            Register(card);
        }

        private void Register(HandCard card)
        {
            if (_cards.Contains(card)) return;
            _cards.Add(card);
            card.OnHoverEnter       += OnCardHoverEnter;
            card.OnHoverExit        += OnCardHoverExit;
            card.OnPointerDownEvent += OnCardPointerDown;

            // 新牌成为最后子物体，确保当前激活牌仍在顶层
            var topCard = _followingCard ?? _selectedCard;
            topCard?.transform.SetAsLastSibling();
        }

        public void RemoveCard(HandCard card)
        {
            if (!_cards.Remove(card)) return;
            card.OnHoverEnter       -= OnCardHoverEnter;
            card.OnHoverExit        -= OnCardHoverExit;
            card.OnPointerDownEvent -= OnCardPointerDown;

            if (_selectedCard == card)  { _selectedCard  = null; _isHolding = false; }
            if (_followingCard == card)   _followingCard  = null;
        }

        // ── 事件处理 ──────────────────────────────────────────────────────
        private void OnCardHoverEnter(HandCard card)
        {
            if (_followingCard != null) return; // follow 期间屏蔽
            _hoveredIndex = _cards.IndexOf(card);
        }

        private void OnCardHoverExit(HandCard card)
        {
            if (_followingCard != null) return;
            int index = _cards.IndexOf(card);
            if (index == _hoveredIndex) _hoveredIndex = -1;
        }

        private void OnCardPointerDown(HandCard card)
        {
            // 如果已有其他选中牌，恢复其层级
            if (_selectedCard != null && _selectedCard != card)
                RestoreSiblingIndex(_selectedCard);

            _selectedCard        = card;
            _isHolding           = true;
            _followingCard       = null;
            _holdStartScreenPos  = Input.mousePosition;

            card.transform.SetAsLastSibling();
        }

        // ── 跟随状态机 ───────────────────────────────────────────────────
        private void UpdateFollowStateMachine()
        {
            if (_selectedCard == null) return;

            if (_followingCard == null)
            {
                if (_isHolding)
                {
                    // ── 按住期间：检测拖拽 ──────────────────────────────
                    if (Input.GetMouseButton(0))
                    {
                        float moved = Vector2.Distance(Input.mousePosition, _holdStartScreenPos);
                        if (moved > dragThreshold)
                        {
                            // 达到拖拽阈值 → 立即进入跟随（拖拽模式）
                            _followingCard = _selectedCard;
                        }
                    }
                    else
                    {
                        // 松开且未拖拽 → 持久选中（click）
                        _isHolding = false;
                    }
                }
                else
                {
                    // ── 持久选中：等待下一次 PointerDown ────────────────
                    // GetMouseButtonDown 同帧可能也触发 OnCardPointerDown（选别的牌），
                    // 若那边已把 _isHolding 设 true，此处就不会走进来（_isHolding true → 上面分支）
                    if (Input.GetMouseButtonDown(0))
                    {
                        _isHolding          = true;
                        _holdStartScreenPos = Input.mousePosition;
                        _followingCard      = _selectedCard; // 持久跟随模式
                        _selectedCard.transform.SetAsLastSibling();
                    }
                }
            }
            else
            {
                // ── 跟随模式（拖拽 或 持久跟随） ────────────────────────
                if (Input.GetMouseButton(0))
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        transform as RectTransform,
                        Input.mousePosition, _eventCamera, out Vector2 localMouse))
                    {
                        _followingCard.HomePosition = localMouse;
                    }
                }
                else
                {
                    // 松开 → 取消选中，飞回
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        transform as RectTransform,
                        Input.mousePosition, _eventCamera, out Vector2 releasePos);

                    var released = _followingCard;
                    _followingCard = null;
                    _isHolding    = false;

                    RestoreSiblingIndex(_selectedCard);
                    _selectedCard = null;
                    _hoveredIndex = -1;

                    OnFollowEnd?.Invoke(released, releasePos);
                }
            }
        }

        // ── 布局计算（每帧） ──────────────────────────────────────────────
        private void RefreshLayout()
        {
            int count = _cards.Count;
            if (count == 0) return;

            float actualSpacing = count > 1
                ? Mathf.Min(cardSpacing, maxTotalWidth / (count - 1)) : 0f;
            float startX = -(count - 1) * actualSpacing * 0.5f;

            // 视觉驱动 Index：悬停 > 选中 > 无
            int activeIndex = _hoveredIndex >= 0 ? _hoveredIndex : GetSelectedIndex();

            for (int i = 0; i < count; i++)
            {
                if (_cards[i] == null) continue;
                var card = _cards[i];

                bool isActive       = (i == activeIndex)
                    || card == _followingCard;
                bool posControlled  = (card == _followingCard); // HomePosition 由状态机控制

                float baseX = startX + i * actualSpacing;

                // 挤开偏移（以 activeIndex 为中心）
                float pushOffset = 0f;
                if (activeIndex >= 0 && i != activeIndex)
                {
                    int distance = i - activeIndex;
                    int absDist  = Mathf.Abs(distance);
                    if (absDist <= pushRadius)
                        pushOffset = (distance > 0 ? 1 : -1) * pushAmount / absDist;
                }

                float normalizedPos = (count > 1)
                    ? (i - (count - 1) * 0.5f) / ((count - 1) * 0.5f) : 0f;
                float arcY = -arcDropY * normalizedPos * normalizedPos;

                if (!posControlled)
                    card.HomePosition = new Vector2(baseX + pushOffset,
                        arcY + (isActive ? hoverRiseY : 0f));

                card.TargetScale    = isActive ? hoverScale : 1f;
                card.TargetRotation = isActive ? 0f : normalizedPos * maxTiltDegrees;
            }
        }

        // ── 工具 ─────────────────────────────────────────────────────────
        private int GetSelectedIndex()
        {
            if (_selectedCard == null) return -1;
            return _cards.IndexOf(_selectedCard);
        }

        private void RestoreSiblingIndex(HandCard card)
        {
            int idx = _cards.IndexOf(card);
            if (idx >= 0) card.transform.SetSiblingIndex(idx);
        }

        // ── 测试发牌 ──────────────────────────────────────────────────────
        [Button]
        public void DebugDealCard()
        {
            _testCardCount++;

            var go = new GameObject($"Card_{_testCardCount}");
            var rt = go.AddComponent<RectTransform>();
            go.transform.SetParent(transform, false);
            rt.sizeDelta        = new Vector2(cardWidth, cardHeight);
            rt.anchoredPosition = dealFromCorner;

            var bg = go.AddComponent<Image>();
            bg.color         = Color.HSVToRGB(UnityEngine.Random.Range(0f, 1f), 0.6f, 0.85f);
            bg.raycastTarget = true;

            AddLabel(go, $"#{_testCardCount}");

            var card          = go.AddComponent<HandCard>();
            card.HomePosition = dealFromCorner;
            AddCard(card);
        }

        private static void AddLabel(GameObject parent, string text)
        {
            var lgo = new GameObject("Label");
            lgo.transform.SetParent(parent.transform, false);

            var lrt       = lgo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;

            var t           = lgo.AddComponent<Text>();
            t.text          = text;
            t.alignment     = TextAnchor.MiddleCenter;
            t.fontSize      = 26;
            t.fontStyle     = FontStyle.Bold;
            t.color         = Color.white;
            t.raycastTarget = false;
        }
    }
}
