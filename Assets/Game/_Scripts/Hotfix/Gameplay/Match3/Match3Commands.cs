using Framework;
using Game.Gateways;
using Game.DTOs;
using UnityEngine;
using Game.Match3;

namespace Game.Gameplay.Match3
{
    /// <summary>
    /// 开始一个三消关卡。会先向服务器请求随机种子。
    /// </summary>
    public class StartMatch3LevelCommand : AbstractCommand
    {
        public int StageId { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Turns { get; set; }

        public override async void Execute(object sender)
        {
            Debug.Log($"[Match3Command] 请求开始关卡: {StageId}");
            
            var gateway = this.GetSystem<IServerGateway>();
            var response = await gateway.PostAsync<StartMatch3Request, StartMatch3Response>("/match3/start", new StartMatch3Request { StageId = StageId });

            if (response != null && response.Code == 0)
            {
                this.GetSystem<IMatch3Service>().StartLevel(Width, Height, Turns, response.Data.RandomSeed);
            }
            else
            {
                Debug.LogError("[Match3Command] 服务器拒绝开启关卡！");
            }
        }
    }

    /// <summary>
    /// 触发一次交换操作
    /// </summary>
    public class Match3SwapCommand : AbstractCommand
    {
        public int X1, Y1, X2, Y2;

        public override void Execute(object sender)
        {
            this.GetSystem<IMatch3Service>().Swap(X1, Y1, X2, Y2);
        }
    }
}
