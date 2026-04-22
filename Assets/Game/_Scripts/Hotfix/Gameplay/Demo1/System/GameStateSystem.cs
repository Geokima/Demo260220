using Framework;
using Framework.Modules.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using static Framework.Logger;

namespace Game.Gameplay.Demo1.System
{
    public interface IGameStateSystem : ISystem
    {
        void SwitchTo(GameState state, int data = 0);
    }

    public class GameStateSystem : AbstractSystem, IGameStateSystem
    {
        private readonly Dictionary<GameState, Type> _panelTypes = new();
        private GameState _currentState = GameState.None;
        private int _currentData;

        public override void Init()
        {
            AutoCollectPanels();
            SwitchTo(GameState.Selection);
        }

        public override void Deinit()
        {
            _panelTypes.Clear();
        }

        public void SwitchTo(GameState state, int data = 0)
        {
            if (_currentState == state)
                return;

            if (_panelTypes.TryGetValue(_currentState, out var oldType))
                ClosePanel(oldType);

            _currentState = state;
            _currentData = data;

            if (_panelTypes.TryGetValue(state, out var newType))
                OpenPanel(newType, data);
        }

        private void OpenPanel(Type panelType, int data)
        {
            var openMethod = typeof(IUISystem).GetMethod("Open")?.MakeGenericMethod(panelType);
            openMethod?.Invoke(this.GetSystem<IUISystem>(), new object[] { data });
        }

        private void ClosePanel(Type panelType)
        {
            var closeMethod = typeof(IUISystem).GetMethod("Close")?.MakeGenericMethod(panelType);
            closeMethod?.Invoke(this.GetSystem<IUISystem>(), null);
        }

        private void AutoCollectPanels()
        {
            var panelTypes = typeof(GameStateSystem).Assembly.GetTypes()
                .Where(t => typeof(UIPanel).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass)
                .SelectMany(t => t.GetCustomAttributes(typeof(GameStateAttribute), false)
                    .Cast<GameStateAttribute>()
                    .Select(attr => new { State = attr.State, PanelType = t }))
                .Where(x => x.State != GameState.None);

            foreach (var item in panelTypes)
            {
                if (_panelTypes.ContainsKey(item.State))
                {
                    LogWarning($"Duplicate [GameState] for {item.State}: {_panelTypes[item.State].Name} -> {item.PanelType.Name}");
                    continue;
                }
                _panelTypes[item.State] = item.PanelType;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class GameStateAttribute : Attribute
    {
        public GameState State { get; }
        public GameStateAttribute(GameState state)
        {
            State = state;
        }
    }
}
