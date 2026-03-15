using UnityEngine;
using Framework;
using Cysharp.Threading.Tasks;

namespace Game.Gameplay.CardBattle
{
    /// <summary>
    /// 后端核心验证沙盒 (Headless Simulator)
    /// 用于在 Unity Editor 中直接验证：纯逻辑运转、表现锁机制、卡牌动作管线
    /// 挂载在任意场景 GameObject 即可运行。
    /// </summary>
    public class CardBattleDemo : MonoBehaviour, IController
    {
        public IArchitecture Architecture { get; set; }
        public IArchitecture GetArchitecture() => Architecture ??= CardBattleArchitecture.Instance;

        private async void Start()
        {
            Debug.Log("<color=cyan>====== [沙盒启动] 杀戮尖塔/怪物火车 底层架构引擎测试 ======</color>");

            // 1. 初始化架构与获取管家
            CardBattleArchitecture.Launch();
            var architecture = CardBattleArchitecture.Instance;
            var model = architecture.GetModel<BattleModel>();
            var queue = architecture.GetSystem<IActionQueueService>();
            var turnService = architecture.GetSystem<ITurnService>();

            // 2. 准备战斗数据 (Mock)
            model.Player = new EntityData("Player", EntityType.Player, "持盾战?", 50);
            var boss = new EntityData("Boss_01", EntityType.Enemy, "腐败邪灵", 200);
            model.Enemies.Add(boss);
            
            for (int i = 0; i < 10; i++) model.DrawPile.Add(new CardData { InstanceId = i.ToString(), Name = "MockCard" });

            // 3. 注册虚拟表现层 (Mock Visualizer)
            RegisterMockVisualizers(architecture, model);

            // 4. 开始战斗流程 (驱动事件和 ActionQueue)
            Debug.Log("<color=green>[Logic] 核心逻辑：发起战斗开始请求，触发第一回合</color>");
            turnService.StartBattle();

            // 手动模拟玩家打出两张“卡牌”注入Action（由于是Headless，就直接Enqueue了）
            Debug.Log("<color=green>[Logic] 核心逻辑：玩家打出 [铁壁] -> 增加 15 点护?, [致死打击] -> 造成 30 点伤?并施加 2 层虚弱</color>");
            
            queue.Enqueue(ActionPool<BlockAction>.Allocate().Init(model.Player, 15));
            queue.Enqueue(ActionPool<DamageAction>.Allocate().Init(model.Player, boss, 30));
            queue.Enqueue(ActionPool<ApplyBuffAction>.Allocate().Init(boss, "Weak", 2));

            // 发出回合结束信号 => 将自动触发怪物行动！
            Debug.Log("<color=green>[Logic] 核心逻辑：玩家宣布回合结束！交接控制权！</color>");
            turnService.EndPlayerTurn();

            // 5. 【核心见证时刻】驱动这无数的瞬间逻辑，并由表现锁挂起
            Debug.Log("<color=magenta>====== [引擎驱动] 开始吞吐管线，下放至表现层 ======</color>");
            await queue.ProcessQueueAsync();

            Debug.Log("<color=cyan>====== [沙盒测试结束] ======</color>");
            Debug.Log($"结算验证 - 玩家护甲: {model.Player.Block.Value} (预期: 5 - 因为怪物会攻击扣除10点)");
            Debug.Log($"结算验证 - Boss血量: {boss.CurrentHp.Value} (预期: 170 - 吃到30点伤害)");
            Debug.Log($"结算验证 - Boss Buff: {boss.Buffs.Count} 种, 虚弱层数: {architecture.GetSystem<IBuffService>().GetBuffStack(boss, "Weak")}");
            
            // 清理
            queue.Clear();
            architecture.Shutdown();
        }

        private void RegisterMockVisualizers(IArchitecture architecture, BattleModel model)
        {
            // 订阅回合事件（瞬间表现）
            architecture.RegisterEvent<TurnStartEvent>(e => Debug.Log($"<color=yellow>[View] 回合开始视觉特效! 轮到: {e.CurrentTurn}, 回合数: {e.TurnCount}</color>"));
            architecture.RegisterEvent<TurnEndEvent>(e => Debug.Log($"<color=yellow>[View] 回合结束视觉特效! 挂机: {e.CurrentTurn}</color>"));

            // 订阅受到攻击（异步挂起）
            architecture.RegisterEvent<DamageVisualEvent>(async e =>
            {
                model.VisualLockCount.Value++;
                Debug.Log($"<color=orange>  > [View] 播放受击动画... [{e.Target.Name}] 受到 {e.Amount} 伤?</color>");
                await UniTask.Delay(800);
                Debug.Log($"<color=orange>  > [View] [受击] 动画放完了！解锁！</color>");
                model.VisualLockCount.Value--;
            });

            // 订阅格挡（异步挂起）
            architecture.RegisterEvent<BlockVisualEvent>(async e =>
            {
                model.VisualLockCount.Value++;
                Debug.Log($"<color=teal>  > [View] 播放护盾生成特效... [{e.Target.Name}] 获得了 {e.Amount} 护盾</color>");
                await UniTask.Delay(500);
                Debug.Log($"<color=teal>  > [View] [护盾] 动画放完了！解锁！</color>");
                model.VisualLockCount.Value--;
            });

            // 订阅加Buff（异步挂起）
            architecture.RegisterEvent<BuffVisualEvent>(async e =>
            {
                model.VisualLockCount.Value++;
                Debug.Log($"<color=purple>  > [View] 播放Buff附加特效... [{e.Target.Name}] 被挂上了 {e.Value} 层 {e.BuffId}!</color>");
                await UniTask.Delay(500); // 稍微快一点
                Debug.Log($"<color=purple>  > [View] [Buff] 动画放完了！解锁！</color>");
                model.VisualLockCount.Value--;
            });
        }
    }
}
