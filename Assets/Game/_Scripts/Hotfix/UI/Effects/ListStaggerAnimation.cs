using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

namespace Game.UI
{
    /// <summary>
    /// 列表子物体的交错入场/出场动画。
    ///
    /// 开：enabled = true  → OnEnable → 入场动画
    /// 关：enabled = false → OnDisable → 出场动画（DOTween SetUpdate 不依赖组件 enabled）
    ///
    /// 若快速切换 enabled，OnEnable 会 KillAllChildTweens 取消进行中的出场动画，反之亦然。
    /// </summary>
    public class ListStaggerAnimation : MonoBehaviour
    {
        [Header("Animation Settings")]
        public float duration      = 0.4f;
        public float delayPerItem  = 0.05f;
        public float startScale    = 0.8f;
        public Vector2 moveOffset  = new Vector2(0, -40f);
        public Ease openEase       = Ease.OutBack;
        public Ease closeEase      = Ease.InQuad;

        [Header("Control")]
        public bool playOnEnable = true;

        [Header("References (Optional)")]
        [Tooltip("完全展开后才启用，关闭时立刻禁用")]
        public DockScaleEffect dockScaleEffect;
        [Tooltip("动画期间禁用，完全展开后才启用")]
        public LayoutGroup layoutGroup;

        // ── 内部状态 ──────────────────────────────────────────────────
        private readonly Dictionary<RectTransform, Vector2> _originPos = new();

        /// <summary>
        /// 每次进入 PlayOpen 都递增，用于使旧轮次的 OnComplete 闭包失效。
        /// </summary>
        private int _openId = 0;

        // ── 生命周期 ──────────────────────────────────────────────────
        private void OnEnable()
        {
            // 先 Kill 掉可能残留的出场动画 Tween
            KillAllChildTweens();

            // 强制让 LayoutGroup 先算一次版，确保子物体 anchoredPosition 是真实布局后的值
            // 必须在 RecordPositions 之前做，否则第一次启用时位置全是 (0,0)
            if (layoutGroup != null)
            {
                layoutGroup.enabled = true;
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
            }

            // 位置已由 LayoutGroup 确定后，才能正确记录原始位置
            RecordPositions();

            // 再禁用 Dock 和 Layout，进入动画阶段
            SetDockAndLayout(false);

            if (playOnEnable) PlayOpen();
        }

        private void OnDisable()
        {
            // 使所有待执行的 Open OnComplete 失效
            _openId++;

            SetDockAndLayout(false);
            KillAllChildTweens();

            // 启动出场动画
            // DOTween 的 SetUpdate(true) 使 Tween 不依赖本组件的 enabled 状态
            // 只要 GameObject 是 Active 的，动画就会跑完
            RunCloseAnimation();
        }

        // ── 工具方法 ──────────────────────────────────────────────────
        private void RecordPositions()
        {
            _originPos.Clear();
            foreach (Transform child in transform)
            {
                if (child is not RectTransform rt) continue;
                _originPos[rt] = rt.anchoredPosition;
                // 主动确保每个子物体都有 CanvasGroup，避免动画时临时添加
                if (rt.GetComponent<CanvasGroup>() == null)
                    rt.gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void SetDockAndLayout(bool state)
        {
            if (dockScaleEffect != null) dockScaleEffect.enabled = state;
            if (layoutGroup     != null) layoutGroup.enabled     = state;
        }

        private void KillAllChildTweens()
        {
            foreach (Transform child in transform)
            {
                if (child is not RectTransform rt) continue;
                rt.DOKill();
                var cg = rt.GetComponent<CanvasGroup>();
                if (cg != null) cg.DOKill();
            }
        }

        private void ResetAllImmediate()
        {
            foreach (Transform child in transform)
            {
                if (child is not RectTransform rt) continue;
                rt.localScale = Vector3.one * startScale;
                if (_originPos.TryGetValue(rt, out var origin))
                    rt.anchoredPosition = origin + moveOffset;
                var cg = rt.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 0f;
            }
        }

        // ── 入场动画 ──────────────────────────────────────────────────
        [ContextMenu("Play Open")]
        public void PlayOpen()
        {
            SetDockAndLayout(false);
            KillAllChildTweens();

            int capturedId = ++_openId;

            int total = 0;
            foreach (Transform c in transform)
                if (c is RectTransform) total++;

            if (total == 0) { OnFullyOpened(); return; }

            int finished = 0;
            int index    = 0;

            foreach (Transform child in transform)
            {
                if (child is not RectTransform rt) continue;

                Vector2 targetPos = _originPos.TryGetValue(rt, out var op) ? op : rt.anchoredPosition;

                rt.anchoredPosition = targetPos + moveOffset;
                rt.localScale       = Vector3.one * startScale;

                var cg = rt.GetComponent<CanvasGroup>();
                cg.alpha = 0f;

                float delay = index * delayPerItem;

                rt.DOAnchorPos(targetPos, duration)
                    .SetDelay(delay).SetEase(openEase).SetUpdate(true)
                    .OnComplete(() =>
                    {
                        if (capturedId != _openId) return; // 旧轮次，丢弃
                        finished++;
                        if (finished >= total) OnFullyOpened();
                    });

                rt.DOScale(Vector3.one, duration)
                    .SetDelay(delay).SetEase(openEase).SetUpdate(true);

                DOTween.To(() => cg.alpha, x => cg.alpha = x, 1f, duration)
                    .SetDelay(delay).SetUpdate(true);

                index++;
            }
        }

        private void OnFullyOpened()
        {
            if (layoutGroup != null)
            {
                layoutGroup.enabled = true;
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
                if (dockScaleEffect != null) dockScaleEffect.RefreshChildren();
            }
            if (dockScaleEffect != null) dockScaleEffect.enabled = true;
        }

        // ── 出场动画 ──────────────────────────────────────────────────
        /// <summary>
        /// 外部统一调此方法关闭，等价于 enabled = false。
        /// 出场动画由 OnDisable 内的 RunCloseAnimation 驱动。
        /// </summary>
        [ContextMenu("Play Close")]
        public void PlayClose() => this.enabled = false;

        private void RunCloseAnimation()
        {
            float closeDuration = duration * 0.7f;

            // 预先统计需要参与动画的数量
            int total = 0;
            foreach (Transform child in transform)
            {
                if (child is not RectTransform rt) continue;
                var cg = rt.GetComponent<CanvasGroup>();
                if (cg != null && cg.alpha >= 0.05f) total++;
            }

            if (total == 0) { ResetAllImmediate(); return; }

            int finished = 0;

            foreach (Transform child in transform)
            {
                if (child is not RectTransform rt) continue;

                var cg = rt.GetComponent<CanvasGroup>();

                // 未出场的 Item 直接锁死隐藏，不播动画
                if (cg.alpha < 0.05f)
                {
                    cg.alpha      = 0f;
                    Vector2 o     = _originPos.TryGetValue(rt, out var op) ? op : rt.anchoredPosition;
                    rt.anchoredPosition = o + moveOffset;
                    rt.localScale = Vector3.one * startScale;
                    continue;
                }

                Vector2 origin    = _originPos.TryGetValue(rt, out var originPos) ? originPos : rt.anchoredPosition;
                Vector2 targetPos = origin + moveOffset;

                rt.DOAnchorPos(targetPos, closeDuration).SetEase(closeEase).SetUpdate(true);
                rt.DOScale(Vector3.one * startScale, closeDuration).SetEase(closeEase).SetUpdate(true);

                DOTween.To(() => cg.alpha, x => cg.alpha = x, 0f, closeDuration)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        finished++;
                        // 所有出场动画完成，同步清理到初始状态
                        if (finished >= total) ResetAllImmediate();
                    });
            }
        }
    }
}
