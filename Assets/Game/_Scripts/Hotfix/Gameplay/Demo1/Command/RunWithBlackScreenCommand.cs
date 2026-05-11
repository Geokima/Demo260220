using System;
using Cysharp.Threading.Tasks;
using Framework;
using Framework.Modules.UI;

namespace Game.Gameplay.Demo1.Command
{
    public class RunWithBlackScreenCommand : AbstractCommand
    {
        public int DelayMsBeforeAction = (int)(UI_BlackScreen.DefaultFadeDuration * 1000);
        public int DelayMsAfterAction = 500;
        public Func<UniTask> Action;

        public override async void Execute(object sender)
        {
            var uiSystem = this.GetSystem<IUISystem>();
            uiSystem.Open<UI_BlackScreen>();

            if (DelayMsBeforeAction > 0)
                await UniTask.Delay(DelayMsBeforeAction);

            if (Action != null)
                await Action.Invoke();

            if (DelayMsAfterAction > 0)
                await UniTask.Delay(DelayMsAfterAction);

            uiSystem.Close<UI_BlackScreen>();
        }
    }
}
