using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Gameplay.Demo1.UI
{
    public class UI_TrashZone : UI_SlotZoneBase
    {
        [SerializeField] private int _priority = 100;
        public override int Priority => _priority;

        protected override int MinCapacity => 1;
        protected override int MaxCapacity => 1;

        public override bool CanAccept(UIDragPayload payload, PointerEventData eventData)
        {
            if (payload == null || payload.View == null)
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

            var view = payload.View;
            if (view == null)
                return false;

            if (view.OwnerZone is UI_CardBoard oldBoard)
                oldBoard.RemoveCard(view);

            UnityEngine.Object.Destroy(view.gameObject);
            return true;
        }
    }
}
