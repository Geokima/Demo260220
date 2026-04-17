using Framework;
using Framework.Modules.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Demo1.System
{
    public interface ISceneModeSystem : ISystem
    {
        void RegisterPanel<T>() where T : UIPanel;
        void SwitchTo(SceneMode mode);
    }

    public class SceneModeSystem : AbstractSystem, ISceneModeSystem
    {
        private readonly Dictionary<SceneMode, Type> _panelTypes = new();
        private SceneMode _currentMode = SceneMode.None;

        public override void Init()
        {
            RegisterPanel<UI_SelectScenePanel>();
        }

        public void RegisterPanel<T>() where T : UIPanel
        {
            var attrs = typeof(T).GetCustomAttributes(typeof(ScenePanelAttribute), false);
            if (attrs.Length > 0)
            {
                var attr = (ScenePanelAttribute)attrs[0];
                _panelTypes[attr.Mode] = typeof(T);
            }
            else
            {
                Debug.LogWarning($"UIPanel {typeof(T).Name} missing [ScenePanel] attribute");
            }
        }

        public void SwitchTo(SceneMode mode)
        {
            if (_currentMode == mode || !_panelTypes.ContainsKey(mode))
                return;

            if (_panelTypes.TryGetValue(_currentMode, out var oldType))
                ClosePanel(oldType);

            _currentMode = mode;

            if (_panelTypes.TryGetValue(mode, out var newType))
                OpenPanel(newType);
        }

        private void OpenPanel(Type panelType)
        {
            var openMethod = typeof(IUISystem).GetMethod("Open")?.MakeGenericMethod(panelType);
            openMethod?.Invoke(this.GetSystem<IUISystem>(), new object[] { null });
        }

        private void ClosePanel(Type panelType)
        {
            var closeMethod = typeof(IUISystem).GetMethod("Close")?.MakeGenericMethod(panelType);
            closeMethod?.Invoke(this.GetSystem<IUISystem>(), null);
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ScenePanelAttribute : Attribute
    {
        public SceneMode Mode { get; }
        public ScenePanelAttribute(SceneMode mode)
        {
            Mode = mode;
        }
    }
}
