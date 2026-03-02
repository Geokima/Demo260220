using UnityEngine;
using Framework;

namespace Framework.Modules.UI
{
    /// <summary>
    /// UI 面板基类
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIPanel : MonoBehaviour, IController
    {
        /// <inheritdoc />
        public IArchitecture Architecture { get; set; }

        [Header("UI Settings")]
        [SerializeField] protected UILayer _layer = UILayer.Window;
        [SerializeField] protected bool _isSingleton = true;
        [SerializeField] protected bool _fixedOrder = false;

        private Canvas _canvas;
        private CanvasGroup _canvasGroup;

        /// <summary>
        /// 面板所属层级
        /// </summary>
        public UILayer Layer => _layer;

        /// <summary>
        /// 是否为单例面板（同一个类只能打开一个实例）
        /// </summary>
        public bool IsSingleton => _isSingleton;

        /// <summary>
        /// 是否固定层级顺序（不随打开顺序调整 Order）
        /// </summary>
        public bool FixedOrder => _fixedOrder;

        /// <summary>
        /// 面板画布组件
        /// </summary>
        public Canvas Canvas => _canvas ??= GetComponent<Canvas>();

        /// <summary>
        /// 面板画布组组件
        /// </summary>
        public CanvasGroup CanvasGroup => _canvasGroup ??= GetComponent<CanvasGroup>();

        /// <summary>
        /// 面板打开时调用
        /// </summary>
        /// <param name="data">初始化数据</param>
        public virtual void OnOpen(object data = null) { }

        /// <summary>
        /// 面板被暂停时调用（如被其他导航面板遮挡）
        /// </summary>
        public virtual void OnPause() { }

        /// <summary>
        /// 面板恢复时调用（如遮挡它的面板关闭）
        /// </summary>
        public virtual void OnResume() { }

        /// <summary>
        /// 面板关闭时调用
        /// </summary>
        public virtual void OnClose() { }
    }
}
