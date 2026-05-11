using Framework;
using Game.Gameplay.Demo1.System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Gameplay.Demo1.UI.Widget
{
    public class Widget_TrashZone : Widget_SlotZoneBase
    {
        [SerializeField] private int _priority = 100;
        public override int Priority => _priority;

        protected override int MinCapacity => 1;
        protected override int MaxCapacity => 1;

        public override bool CanAccept(DragPayload payload, PointerEventData eventData)
        {
            if (payload == null || payload.View == null)
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

            var view = payload.View;
            if (view == null)
                return false;

            var model = Demo1Architecture.Instance.GetModel<Demo1Model>();
            var sellPrice = view.Model?.Price?.Value ?? 0;
            if (sellPrice > 0)
                model.Gold.Value += sellPrice;

            if (view.OwnerZone is Widget_CardBoard oldBoard)
                oldBoard.RemoveCard(view);

            UnityEngine.Object.Destroy(view.gameObject);
            return true;
        }
    }
}
