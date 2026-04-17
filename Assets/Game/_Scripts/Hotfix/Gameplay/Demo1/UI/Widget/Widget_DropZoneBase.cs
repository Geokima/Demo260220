using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Gameplay.Demo1.UI.Widget
{
    public abstract class Widget_DropZoneBase : MonoBehaviour
    {
        private static readonly List<Widget_DropZoneBase> _activeZones = new List<Widget_DropZoneBase>();

        public virtual int Priority => 0;
        public abstract bool CanAccept(DragPayload payload, PointerEventData eventData);
        public abstract bool Accept(DragPayload payload, PointerEventData eventData);

        public static IReadOnlyList<Widget_DropZoneBase> ActiveZones => _activeZones;

        protected virtual void OnEnable()
        {
            if (!_activeZones.Contains(this))
                _activeZones.Add(this);
        }

        protected virtual void OnDisable()
        {
            _activeZones.Remove(this);
        }
    }
}
