using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Game.UI
{
    /// <summary>
    /// 苹果 Dock 栏缩放效果
    /// - 鼠标悬停时，最近的子物体放大，两侧按距离渐进衰减，形成波浪感
    /// - 通过 LayoutElement.preferredSize 驱动 LayoutGroup 自动挤开邻居
    /// - 挂载在含有 HorizontalLayoutGroup 或 VerticalLayoutGroup 的父物体上
    /// </summary>
    [RequireComponent(typeof(HorizontalOrVerticalLayoutGroup))]
    public class DockScaleEffect : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        // ── Inspector ────────────────────────────────────────────────
        [Header("缩放设置")]
        [Tooltip("鼠标正中心的最大缩放倍率")]
        public float maxScale = 1.5f;

        [Tooltip("影响半径（父物体本地坐标系，单位与 RectTransform 一致）")]
        public float effectRadius = 180f;

        [Tooltip("缩放过渡速度（越大越快）")]
        public float transitionSpeed = 14f;

        [Header("缩放曲线")]
        [Tooltip("X=归一化距离(0=中心,1=边缘), Y=缩放权重(0~1)\n留空则默认使用 EaseInOut")]
        public AnimationCurve falloffCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        // ── 内部数据 ──────────────────────────────────────────────────
        private enum DockAxis { Horizontal, Vertical }

        private struct ChildInfo
        {
            public RectTransform Rect;
            public LayoutElement Layout;
            public Vector2       BaseSize;      // LayoutGroup 布局稳定后的原始尺寸
            public float         CurrentScale;  // 平滑插值中间值
        }

        private List<ChildInfo> _children      = new();
        private DockAxis        _axis          = DockAxis.Horizontal;
        private bool            _pointerInside = false;
        private Vector2         _localMousePos;
        private RectTransform   _rectTransform;

        // ── 生命周期 ──────────────────────────────────────────────────
        private void Awake()
        {
            _rectTransform = transform as RectTransform;

            // 自动检测 Dock 方向
            _axis = GetComponent<VerticalLayoutGroup>() != null
                ? DockAxis.Vertical
                : DockAxis.Horizontal;
        }

        private void OnEnable()
        {
            RefreshChildren();
        }

        private void OnDisable()
        {
            _pointerInside = false;

            // 硬重置所有子物体，防止残留放大状态
            for (int i = 0; i < _children.Count; i++)
            {
                var info = _children[i];
                info.CurrentScale = 1f;
                _children[i] = info;

                if (info.Rect == null) continue;
                info.Rect.localScale = Vector3.one;

                if (info.Layout != null)
                {
                    info.Layout.preferredWidth  = -1;
                    info.Layout.preferredHeight = -1;
                }
            }

            if (_rectTransform != null)
                LayoutRebuilder.MarkLayoutForRebuild(_rectTransform);
        }

        // ── 子物体初始化 ──────────────────────────────────────────────
        /// <summary>
        /// 重新扫描所有子物体，记录其 LayoutGroup 布局稳定后的原始尺寸。
        /// 由外部（如 ListStaggerAnimation 展开完成后）调用，或通过右键菜单手动触发。
        /// </summary>
        [ContextMenu("刷新子物体")]
        public void RefreshChildren()
        {
            _children.Clear();

            foreach (Transform child in transform)
            {
                if (child is not RectTransform rt) continue;

                var le = rt.GetComponent<LayoutElement>();
                if (le == null)
                    le = rt.gameObject.AddComponent<LayoutElement>();

                _children.Add(new ChildInfo
                {
                    Rect         = rt,
                    Layout       = le,
                    BaseSize     = rt.rect.size,
                    CurrentScale = rt.localScale.x
                });
            }
        }

        // ── 鼠标事件 ──────────────────────────────────────────────────
        public void OnPointerEnter(PointerEventData e) => _pointerInside = true;
        public void OnPointerExit(PointerEventData e)  => _pointerInside = false;

        public void OnPointerMove(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform, e.position, e.pressEventCamera, out _localMousePos);
        }

        // ── 每帧更新 ──────────────────────────────────────────────────
        private void Update()
        {
            if (_children.Count == 0) return;

            bool dirty = false;

            for (int i = 0; i < _children.Count; i++)
            {
                var info = _children[i];
                if (info.Rect == null) continue;

                // 将子物体世界坐标转换到父物体本地坐标系
                // 与 ScreenPointToLocalPointInRectangle 的结果处于同一坐标系，消除 anchor/pivot 差异
                Vector2 childCenter = _rectTransform.InverseTransformPoint(info.Rect.position);

                // 只取 Dock 轴方向的一维距离（与真实苹果 Dock 行为一致）
                float dist = _pointerInside
                    ? (_axis == DockAxis.Horizontal
                        ? Mathf.Abs(childCenter.x - _localMousePos.x)
                        : Mathf.Abs(childCenter.y - _localMousePos.y))
                    : float.MaxValue;

                // 根据距离和曲线计算目标缩放
                float targetScale = 1f;
                if (_pointerInside && dist < effectRadius)
                {
                    float t      = dist / effectRadius;           // 0=鼠标中心  1=影响边缘
                    float weight = falloffCurve.Evaluate(t);      // 从曲线中取权重
                    targetScale  = Mathf.Lerp(1f, maxScale, weight);
                }

                // 平滑插值当前缩放值
                float smoothed = Mathf.Lerp(info.CurrentScale, targetScale,
                                            Time.deltaTime * transitionSpeed);

                if (Mathf.Abs(smoothed - info.CurrentScale) > 0.0001f)
                    dirty = true;

                info.CurrentScale    = smoothed;
                info.Rect.localScale = Vector3.one * smoothed;

                // 通过 preferredSize 驱动 LayoutGroup 将邻居推开
                if (info.Layout != null)
                {
                    info.Layout.preferredWidth  = info.BaseSize.x * smoothed;
                    info.Layout.preferredHeight = info.BaseSize.y * smoothed;
                }

                _children[i] = info;
            }

            // 仅在有实际变化时才通知 LayoutGroup 重排，降低每帧开销
            if (dirty)
                LayoutRebuilder.MarkLayoutForRebuild(_rectTransform);
        }
    }
}
