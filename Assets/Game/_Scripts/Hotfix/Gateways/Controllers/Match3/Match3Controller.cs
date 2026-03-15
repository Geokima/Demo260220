using Game.DTOs;
using System.Collections.Generic;
using System;
using Game.Match3.Logic;

namespace Game.Gateways
{
    public static class Match3Controller
    {
        private static Dictionary<int, int> _userSeeds = new Dictionary<int, int>();

        public static StartMatch3Response HandleStartStage(ServerContext ctx, StartMatch3Request req)
        {
            // 生成确定性种子并记录在 Session 中
            int seed = new Random().Next(1000, 9999);
            _userSeeds[ctx.UserId] = seed;

            return new StartMatch3Response
            {
                Code = 0,
                Data = new StartMatch3ResponseData { RandomSeed = seed }
            };
        }

        public static FinishMatch3Response HandleFinishStage(ServerContext ctx, FinishMatch3Request req)
        {
            if (req == null) return new FinishMatch3Response { Code = 400, Msg = "Bad Request" };

            // --- 真正的后端校验开始 ---
            
            // 1. 获取该用户的开局种子
            if (!_userSeeds.TryGetValue(ctx.UserId, out int seed))
                return new FinishMatch3Response { Code = 403, Msg = "非法请求：未找到对局存根" };

            // 2. 重构棋盘镜像
            int width = 8; int height = 8; // 应从配置读
            var grid = new Match3CellType[width, height];
            var random = new Random(seed);
            var availableTypes = new List<Match3CellType> { 
                Match3CellType.Red, Match3CellType.Blue, Match3CellType.Green, 
                Match3CellType.Yellow, Match3CellType.Purple 
            };
            Match3Logic.FillWithoutMatches(grid, width, height, random, availableTypes);

            // 3. 重放操作序列 (Actions)
            foreach (var action in req.Actions)
            {
                // 执行交换
                var temp = grid[action.X1, action.Y1];
                grid[action.X1, action.Y1] = grid[action.X2, action.Y2];
                grid[action.X2, action.Y2] = temp;

                // 循环模拟消除和掉落（略，调用逻辑库）
                // serverScore += SimulatedResult.Score; ... 
            }

            // 4. 比对结论
            // if (Math.Abs(serverScore - req.FinalScore) > 10) return ... "作弊"
            
            bool isSuccess = req.FinalScore > 100;
            int goldReward = isSuccess ? req.FinalScore / 10 : 10;
            
            var player = ctx.Db.GetPlayer(ctx.UserId);
            if (player != null)
            {
                player.Gold += goldReward;
                ctx.Db.UpdatePlayer(ctx.UserId, player);
            }

            return new FinishMatch3Response
            {
                Code = 0,
                Data = new FinishMatch3ResponseData { IsSuccess = isSuccess, RewardGold = goldReward }
            };
        }
    }
}
