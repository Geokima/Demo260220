using Framework.Modules.Procedure;
using UnityEngine;
using Game.Match3;
using Game.Gameplay.Match3;
using Framework;

namespace Game.Procedures
{
    public class Match3Procedure : ProcedureBase, IController
    {
        public IArchitecture GetArchitecture() => Match3Architecture.Instance;

        public override void OnEnter()
        {
            Debug.Log("[Match3Procedure] 进入三消关卡...");
            
            // 启动子架构
            Match3Architecture.Launch();

            // 触发开局
            this.SendCommand(new StartMatch3LevelCommand { 
                StageId = 101, Width = 8, Height = 8, Turns = 20 
            });
        }

        public override void OnExit()
        {
            Debug.Log("[Match3Procedure] 退出三消关卡！");
            Match3Architecture.Instance.Shutdown();
        }
    }
}
