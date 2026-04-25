using System.Collections.Generic;
using System.Reflection;
using Framework;
using Framework.Modules.UI;

namespace Game.Gameplay.Demo1.Command
{
    public class ToggleBenchPanelCommand : AbstractCommand
    {
        public override void Execute(object sender)
        {
            var uiSystem = this.GetSystem<IUISystem>();
            if (uiSystem.IsOpen<UI_BenchPanel>())
                uiSystem.Close<UI_BenchPanel>();
            else
                uiSystem.Open<UI_BenchPanel>();
        }
    }
}
