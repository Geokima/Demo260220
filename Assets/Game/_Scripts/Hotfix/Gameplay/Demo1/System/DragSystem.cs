using Framework;
using Game.Gameplay.Demo1;
using Game.Gameplay.Demo1.UI.Widget;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Gameplay.Demo1.System
{
    public interface IDragSystem : ISystem
    {
        bool IsDragging { get; }
        DragPayload CurrentPayload { get; }
        Widget_DropZoneBase CurrentZone { get; }
        Vector2 CurrentPointerScreenPos { get; }
        Camera CurrentEventCamera { get; }

        void BeginDrag(Widget_CardView view, PointerEventData eventData);
        void Drag(PointerEventData eventData);
        void EndDrag(PointerEventData eventData);
        void CancelCurrentDrag();
    }

    public class DragSystem : AbstractSystem, IDragSystem
    {
        private class DragContext
        {
            public DragPayload Payload;
            public RectTransform ViewRectTransform;
            public CanvasGroup ViewCanvasGroup;
            public RectTransform OriginalParent;
            public int OriginalSiblingIndex;
            public Vector2 OriginalAnchoredPosition;
            public Vector2 OriginalAnchorMin;
            public Vector2 OriginalAnchorMax;
            public Vector2 OriginalPivot;
            public Vector2 OriginalSizeDelta;
            public Vector3 OriginalLocalScale;
            public Vector2 PointerOffsetLocal;
            public RectTransform DragLayer;
            public Widget_DropZoneBase CurrentZone;
            public Vector2 PointerScreenPos;
            public Camera EventCamera;
        }

        private DragContext _ctx;

        public bool IsDragging => _ctx != null;
        public DragPayload CurrentPayload => _ctx?.Payload;
        public Widget_DropZoneBase CurrentZone => _ctx?.CurrentZone;
        public Vector2 CurrentPointerScreenPos => _ctx != null ? _ctx.PointerScreenPos : default;
        public Camera CurrentEventCamera => _ctx != null ? _ctx.EventCamera : null;

        public void BeginDrag(Widget_CardView view, PointerEventData eventData)
        {
            if (view == null)
                return;

            if (_ctx != null)
                CancelCurrentDrag();

            var rt = view.RectTransform;
            if (rt == null)
                return;

            var cg = view.CanvasGroup;
            if (cg == null)
                return;

            var canvas = view.GetComponentInParent<Canvas>();
            RectTransform dragLayer = null;
            if (canvas != null && canvas.rootCanvas != null)
                dragLayer = GetOrCreateDragLayer(canvas.rootCanvas);

            if (dragLayer == null)
                dragLayer = rt.parent as RectTransform;

            if (dragLayer == null)
                return;

            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            var centerWorld = Vector3.Lerp(corners[0], corners[2], 0.5f);
            Vector2 centerScreen = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, centerWorld);
            Vector2 pointerOffsetFromCenterScreen = eventData.position - centerScreen;

            _ctx = new DragContext
            {
                Payload = new DragPayload(view, view.Model, view.WidthInCells, pointerOffsetFromCenterScreen),
                ViewRectTransform = rt,
                ViewCanvasGroup = cg,
                OriginalParent = rt.parent as RectTransform,
                OriginalSiblingIndex = rt.GetSiblingIndex(),
                OriginalAnchoredPosition = rt.anchoredPosition,
                OriginalAnchorMin = rt.anchorMin,
                OriginalAnchorMax = rt.anchorMax,
                OriginalPivot = rt.pivot,
                OriginalSizeDelta = rt.sizeDelta,
                OriginalLocalScale = rt.localScale,
                DragLayer = dragLayer,
                PointerScreenPos = eventData.position,
                EventCamera = eventData.pressEventCamera
            };

            rt.SetParent(dragLayer, true);
            rt.SetAsLastSibling();

            cg.blocksRaycasts = false;

            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    dragLayer, eventData.position, eventData.pressEventCamera, out var localPos))
            {
                _ctx.PointerOffsetLocal = localPos - rt.anchoredPosition;
                rt.anchoredPosition = localPos - _ctx.PointerOffsetLocal;
            }
        }

        public void Drag(PointerEventData eventData)
        {
            if (_ctx == null || _ctx.ViewRectTransform == null || _ctx.DragLayer == null)
                return;

            _ctx.PointerScreenPos = eventData.position;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _ctx.DragLayer, eventData.position, eventData.pressEventCamera, out var localPos))
            {
                _ctx.ViewRectTransform.anchoredPosition = localPos - _ctx.PointerOffsetLocal;
            }

            var zone = FindBestZone(eventData);
            if (zone != _ctx.CurrentZone)
            {
                _ctx.CurrentZone = zone;
            }
        }

        public void EndDrag(PointerEventData eventData)
        {
            if (_ctx == null)
                return;

            _ctx.PointerScreenPos = eventData.position;
            var zone = FindBestZone(eventData);
            bool accepted = zone != null && zone.CanAccept(_ctx.Payload, eventData) && zone.Accept(_ctx.Payload, eventData);

            if (accepted)
            {
                if (_ctx.ViewCanvasGroup != null)
                    _ctx.ViewCanvasGroup.blocksRaycasts = true;
                _ctx = null;
                return;
            }

            CancelCurrentDrag();
        }

        public void CancelCurrentDrag()
        {
            if (_ctx == null)
                return;

            if (_ctx.ViewRectTransform != null && _ctx.OriginalParent != null)
            {
                _ctx.ViewRectTransform.SetParent(_ctx.OriginalParent, false);
                _ctx.ViewRectTransform.SetSiblingIndex(_ctx.OriginalSiblingIndex);
                _ctx.ViewRectTransform.anchorMin = _ctx.OriginalAnchorMin;
                _ctx.ViewRectTransform.anchorMax = _ctx.OriginalAnchorMax;
                _ctx.ViewRectTransform.pivot = _ctx.OriginalPivot;
                _ctx.ViewRectTransform.sizeDelta = _ctx.OriginalSizeDelta;
                _ctx.ViewRectTransform.localScale = _ctx.OriginalLocalScale;
                _ctx.ViewRectTransform.anchoredPosition = _ctx.OriginalAnchoredPosition;
            }

            if (_ctx.ViewCanvasGroup != null)
                _ctx.ViewCanvasGroup.blocksRaycasts = true;

            _ctx = null;
        }

        private Widget_DropZoneBase FindBestZone(PointerEventData eventData)
        {
            if (_ctx == null || _ctx.Payload == null)
                return null;

            Widget_DropZoneBase best = null;
            int bestPriority = int.MinValue;

            var zones = Widget_DropZoneBase.ActiveZones;
            for (int i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                if (zone == null || !zone.isActiveAndEnabled)
                    continue;

                var zoneRect = zone.GetComponent<RectTransform>();
                if (zoneRect == null)
                    continue;

                if (!RectTransformUtility.RectangleContainsScreenPoint(zoneRect, eventData.position, eventData.pressEventCamera))
                    continue;

                if (!zone.CanAccept(_ctx.Payload, eventData))
                    continue;

                if (zone.Priority > bestPriority)
                {
                    bestPriority = zone.Priority;
                    best = zone;
                }
            }

            return best;
        }

        private RectTransform GetOrCreateDragLayer(Canvas canvas)
        {
            var existing = canvas.transform.Find("[DragLayer]");
            if (existing != null)
                return existing as RectTransform;

            var go = new GameObject("[DragLayer]");
            go.transform.SetParent(canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            var dragCanvas = go.AddComponent<Canvas>();
            dragCanvas.overrideSorting = true;
            dragCanvas.sortingOrder = 3000;

            //go.AddComponent<GraphicRaycaster>();

            return rt;
        }
    }
}
