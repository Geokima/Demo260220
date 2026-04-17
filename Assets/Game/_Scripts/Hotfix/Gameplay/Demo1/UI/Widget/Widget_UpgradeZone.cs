using Game.Gameplay.Demo1;
using Game.Gameplay.Demo1.System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Gameplay.Demo1.UI.Widget
{
    public class Widget_UpgradeZone : Widget_SlotZoneBase
    {
        [SerializeField] private int _priority = 50;
        public override int Priority => _priority;

        protected override int MinCapacity => 1;
        protected override int MaxCapacity => 1;

        public override bool CanAccept(DragPayload payload, PointerEventData eventData)
        {
            if (payload == null || payload.Model == null)
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

            var model = payload.Model;
            var rank = model.Rank.Value;
            if (rank < CardRank.Diamond)
                model.Rank.Value = (CardRank)((int)rank + 1);

            return false;
        }
    }
}
