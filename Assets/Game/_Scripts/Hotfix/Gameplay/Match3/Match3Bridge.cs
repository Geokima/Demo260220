using Framework;
using Framework.Modules.Network;
using Game.DTOs;
using Game.Gateways;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Game.Match3
{
    public class Match3Bridge : MonoBehaviour, IController
    {
        public IArchitecture Architecture { get; set; }
        public IArchitecture GetArchitecture() => Architecture ??= GameArchitecture.Instance;

        private void Start()
        {
            Match3Architecture.Instance.RegisterEvent<Match3GameOverEvent>(e => OnGameOver(e).Forget());
        }

        private async UniTaskVoid OnGameOver(Match3GameOverEvent e)
        {
            Debug.Log($"[Match3Bridge] 收到三消结束事件: 胜利={e.IsWin}, 最终得分={e.Score}");

            var gateway = this.GetSystem<IServerGateway>();
            var model = Match3Architecture.Instance.GetModel<Match3Model>();
            
            var req = new FinishMatch3Request { 
                StageId = 1, 
                FinalScore = e.Score,
                Actions = model.ActionHistory
            };
            var res = await gateway.PostAsync<FinishMatch3Request, FinishMatch3Response>("/match3/finish", req);

            if (res.Code == 0 && res.Data.IsSuccess)
            {
                Debug.Log($"[Match3Bridge] 结算成功！获得金币: {res.Data.RewardGold}");
            }
            
            Match3Architecture.Instance.Shutdown();
            // 切回主界面或其他逻辑...
        }
    }
}
