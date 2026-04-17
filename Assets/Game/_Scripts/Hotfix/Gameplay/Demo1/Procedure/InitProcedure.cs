using Cysharp.Threading.Tasks;
using Framework.Modules.Procedure;
using Framework.Modules.UI;

namespace Game.Gameplay.Demo1.Procedure
{
    public class InitProcedure : ProcedureBase
    {
        public async override void OnEnter()
        {
            await UniTask.Delay(1000);
            Architecture.GetSystem<IUISystem>().Open<UI_Demo1PlayerPanel>();
            ChangeProcedure<SelectionProcedure>();
        }
    }
}
