using UnityEngine;

namespace Framework.Modules.UI
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIPanel : MonoBehaviour
    {
        [SerializeField] protected UILayer _layer = UILayer.Window;
        [SerializeField] protected bool _isSingleton = true;
        [SerializeField] protected bool _fixedOrder = false;

        private Canvas _canvas;
        private CanvasGroup _canvasGroup;

        public UILayer Layer => _layer;
        public bool IsSingleton => _isSingleton;
        public bool FixedOrder => _fixedOrder;
        public Canvas Canvas => _canvas ??= GetComponent<Canvas>();
        public CanvasGroup CanvasGroup => _canvasGroup ??= GetComponent<CanvasGroup>();

        protected virtual void Awake()
        {
            if (CanvasGroup == null)
                gameObject.AddComponent<CanvasGroup>();
        }

        public virtual void OnOpen(object data = null) { }
        public virtual void OnPause() { }
        public virtual void OnResume() { }
        public virtual void OnClose() { }
    }
}
