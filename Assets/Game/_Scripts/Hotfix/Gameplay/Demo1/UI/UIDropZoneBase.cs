using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Gameplay.Demo1.UI
{
    public abstract class UIDropZoneBase : MonoBehaviour
    {
        private static readonly List<UIDropZoneBase> _activeZones = new List<UIDropZoneBase>();

        public virtual int Priority => 0;
        public abstract bool CanAccept(UIDragPayload payload, PointerEventData eventData);
        public virtual void Preview(UIDragPayload payload, PointerEventData eventData) { }
        public virtual void CancelPreview(UIDragPayload payload) { }
        public abstract bool Accept(UIDragPayload payload, PointerEventData eventData);

        public static IReadOnlyList<UIDropZoneBase> ActiveZones => _activeZones;

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
