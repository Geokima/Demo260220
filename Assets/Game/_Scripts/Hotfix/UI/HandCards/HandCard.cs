using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace Game.UI
{
    /// <summary>
    /// 手牌单张。只负责平滑 Lerp 到 HandLayout 设定的目标值，并上报 Hover / PointerDown 事件。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class HandCard : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler
    {
        // ── 由 HandLayout 每帧写入 ───────────────────────────────────────
        public Vector2 HomePosition;
        public float   TargetScale    = 1f;
        public float   TargetRotation = 0f;

        // ── 运动参数 ─────────────────────────────────────────────────────
        [Header("运动")]
        public float moveSpeed   = 6f;
        public float scaleSpeed  = 7f;
        public float rotateSpeed = 5f;

        // ── 状态 ─────────────────────────────────────────────────────────
        public bool IsHovered { get; private set; }

        // ── 事件 ─────────────────────────────────────────────────────────
        public Action<HandCard> OnHoverEnter;
        public Action<HandCard> OnHoverExit;
        /// <summary>按下时通知 HandLayout，由 HandLayout 决定是否选中</summary>
        public Action<HandCard> OnPointerDownEvent;

        // ── 内部 ─────────────────────────────────────────────────────────
        private RectTransform _rectTransform;

        private void Awake() => _rectTransform = transform as RectTransform;

        private void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);

            _rectTransform.anchoredPosition = Vector2.Lerp(
                _rectTransform.anchoredPosition, HomePosition, dt * moveSpeed);

            float smoothScale = Mathf.Lerp(
                _rectTransform.localScale.x, TargetScale, dt * scaleSpeed);
            _rectTransform.localScale = Vector3.one * smoothScale;

            float currentRot = _rectTransform.localEulerAngles.z;
            if (currentRot > 180f) currentRot -= 360f;
            _rectTransform.localEulerAngles = new Vector3(0f, 0f,
                Mathf.Lerp(currentRot, TargetRotation, dt * rotateSpeed));
        }

        public void OnPointerEnter(PointerEventData e) { IsHovered = true;  OnHoverEnter?.Invoke(this); }
        public void OnPointerExit(PointerEventData e)  { IsHovered = false; OnHoverExit?.Invoke(this);  }
        public void OnPointerDown(PointerEventData e)  => OnPointerDownEvent?.Invoke(this);
    }
}
