using UnityEngine;
using Framework;
using Game.Gameplay.CardBattle;
using Game.Config;
using System.Collections.Generic;
using Framework.Modules.Config;
using Cysharp.Threading.Tasks;
using Game.Procedures;

// 将空间回归到 Gameplay 下
namespace Game.Gameplay.CardBattle
{
    /// <summary> 
    /// UI表现层与后端逻辑管线的桥接器 (Syncer)
    /// 开启 OnGUI 模式用于快速在 Unity 原生画面中进行联调测试。
    /// 正规项目中，此处将对接 UGUI 系统及网络消息派发。
    /// </summary>
    public class CardBattleTestController : MonoBehaviour, IController
    {
        public IArchitecture Architecture { get; set; }
        
        // 我们利用 IController 钩子去抓取外层主框架的 App 级配置：GameArchitecture
        public IArchitecture GetArchitecture() => GameArchitecture.Instance; 
        
        // 我们同时在这里缓存子架构（CardBattle子空间）
        private IArchitecture GetBattleArch() => CardBattleArchitecture.Instance;

        private BattleModel _model;
        private IActionQueueService _queue;
        private ITurnService _turnService;
        private IConfigSystem _configSystem;

        private void Awake()
        {
            Architecture = GameArchitecture.Instance;
            Architecture.RegisterEvent<PreloadCompleteEvent>(OnPreloadComplete);
        }

        private void OnDestroy()
        {
            Architecture?.UnregisterEvent<PreloadCompleteEvent>(OnPreloadComplete);
            
            var arch = CardBattleArchitecture.Instance;
            arch?.GetSystem<IActionQueueService>()?.Clear();
            arch?.Shutdown();
        }

        private void OnPreloadComplete(PreloadCompleteEvent e)
        {
            Debug.Log("<color=cyan>[CardBattleTestController] 配置加载完毕，拉起基于表数据的真实战局...</color>");
            
            // 1. 点火子架构
            CardBattleArchitecture.Launch();

            _model = GetBattleArch().GetModel<BattleModel>();
            _queue = GetBattleArch().GetSystem<IActionQueueService>();
            _turnService = GetBattleArch().GetSystem<ITurnService>();
            
            // 2. 获取主架构里的配置系统（读取我们写的 cfg_card.json）
            _configSystem = GetArchitecture().GetSystem<IConfigSystem>();

            // 3. Mock 战斗参战数据
            _model.Player = new EntityData("Player_1", EntityType.Player, "持盾战士", 50);
            var boss = new EntityData("Enemy_1", EntityType.Enemy, "腐败邪灵", 200);
            _model.Enemies.Add(boss);

            // 预设一个足够大的测试牌库 (15张)，避免洗牌后数值看起来没变
            int[] testIds = { 1001, 1001, 1001, 1002, 1002, 1002, 1003, 1004, 1004, 1005, 1006, 1006, 1001, 1002, 1004 };
            foreach(var id in testIds) _model.DrawPile.Add(CreateCardData(id));

            // 4. 重头戏：监听异步系统消息用来挂起操作
            RegisterVisualizers();
            
            // 5. 宣战（初始化回合）
            _turnService.StartBattle(); 

            // 重要：启动时立即驱动一次管线
            _queue.ProcessQueueAsync().Forget();
        }

        private List<string> _battleLogs = new List<string>();
        private void AddLog(string msg)
        {
            _battleLogs.Insert(0, $"[{UnityEngine.Time.frameCount}] {msg}");
            if (_battleLogs.Count > 8) _battleLogs.RemoveAt(8);
        }

        private CardData CreateCardData(int configId)
        {
            var config = _configSystem.Get<CardConfig>(configId);
            int cost = config?.Cost ?? 1;
            
            var cardData = new CardData()
            {
                InstanceId = System.Guid.NewGuid().ToString(),
                ConfigId = configId,
                Name = config?.Name ?? "丢失的数据",
                TargetType = config?.TargetType == "SingleEnemy" ? CardTargetType.SingleEnemy : CardTargetType.Self 
            };
            cardData.BaseCost.Value = cost;
            cardData.CurrentCost.Value = cost;
            
            UpdateCardDescription(cardData);
            return cardData;
        }

        private void UpdateCardDescription(CardData card)
        {
            var config = _configSystem.Get<CardConfig>(card.ConfigId);
            if (config == null || string.IsNullOrEmpty(config.Desc)) return;

            var values = new List<object>();
            foreach (var effect in config.Effects)
            {
                if (effect.Type == "Damage")
                {
                    // 动态计算当前的预计伤害数值
                    int final = GetBattleArch().GetSystem<INumericalService>().CalculateDamage(_model.Player, null, effect.Value);
                    string color = final > effect.Value ? "green" : (final < effect.Value ? "red" : "white");
                    values.Add($"<color={color}>{final}</color>");
                }
                else if (effect.Type == "Block")
                {
                    int final = GetBattleArch().GetSystem<INumericalService>().CalculateBlock(_model.Player, effect.Value);
                    string color = final > effect.Value ? "green" : (final < effect.Value ? "red" : "white");
                    values.Add($"<color={color}>{final}</color>");
                }
                else
                {
                    values.Add(effect.Value);
                }
            }

            try {
                card.Description.Value = string.Format(config.Desc, values.ToArray());
            } catch {
                card.Description.Value = config.Desc;
            }
        }

        private void UpdateAllHandDescriptions()
        {
            foreach(var card in _model.Hand) UpdateCardDescription(card);
        }

        private void RegisterVisualizers()
        {
            var arch = GetBattleArch();
            
            arch.RegisterEvent<DamageVisualEvent>(async e => {
                AddLog($"{e.Source?.Name ?? "系统"} 对 {e.Target.Name} 造成 {e.Amount} 伤害");
                _model.VisualLockCount.Value++;
                await UniTask.Delay(500);
                _model.VisualLockCount.Value--;
                UpdateAllHandDescriptions(); 
            });
            
            arch.RegisterEvent<BlockVisualEvent>(async e => {
                AddLog($"{e.Target.Name} 获得 {e.Amount} 护甲");
                _model.VisualLockCount.Value++;
                await UniTask.Delay(300);
                _model.VisualLockCount.Value--;
                UpdateAllHandDescriptions();
            });
            
            arch.RegisterEvent<BuffVisualEvent>(async e => {
                AddLog($"{e.Target.Name} 获得 {e.BuffId} x{e.Value}");
                _model.VisualLockCount.Value++;
                await UniTask.Delay(300);
                _model.VisualLockCount.Value--;
                UpdateAllHandDescriptions();
            });
        }

        private async void PlayCard(CardData card)
        {
            // 1. 防抖与逻辑状态检查
            if (_model.VisualLockCount.Value > 0) return;
            
            // 2. 能量检查
            if (_model.Player.Energy.Value < card.CurrentCost.Value)
            {
                Debug.LogWarning($"<color=red>能量不足！需要 {card.CurrentCost.Value}, 当前只有 {_model.Player.Energy.Value}</color>");
                return;
            }
            
            var config = _configSystem.Get<CardConfig>(card.ConfigId);
            if (config == null) return;

            // 3. 构建上下文
            var context = new CardBattleContext { Source = _model.Player, Card = card, Targets = new List<EntityData>() };
            
            // 简单选择器：若是打敌人的牌，我们选定 Boss (即 Enemies[0])
            if (card.TargetType == CardTargetType.SingleEnemy && _model.Enemies.Count > 0)
            {
                context.Targets.Add(_model.Enemies[0]);
            }
            else
            {
                context.Targets.Add(_model.Player); // Self类牌给自己
            }

            // 4. 消耗能量
            _model.Player.Energy.Value -= card.CurrentCost.Value;

            // 5. 通过工厂将 Config 直接映射为【多个 Action 对象集】
            var actions = CardFactory.CreateActions(config, context);

            // 6. 全部入队
            foreach(var act in actions) 
                _queue.Enqueue(act);

            // 7. 从手牌废弃进入墓地
            _model.Hand.Remove(card);
            _model.DiscardPile.Add(card);

            // 8. 驱动管线!
            await _queue.ProcessQueueAsync();
        }

        private void EndTurn()
        {
            if (_model.VisualLockCount.Value > 0) return;
            
            _turnService.EndPlayerTurn(); // 结束回合内部将向队列压入怪物攻击指令
            _queue.ProcessQueueAsync().Forget();
        }

        #region 【商业级快捷真机测试法宝 - 原生 OnGUI() 重写面板】
        private void OnGUI()
        {
            if (_model == null) return;

            // GUI皮肤设置，画在左上角的虚拟屏幕中
            GUILayout.BeginArea(new Rect(10, 10, 400, 750), GUI.skin.box);
            GUILayout.Label($"<b>【卡牌战役测试面板】 - 第 {_model.TurnCount.Value} 回合</b>");
            
            // 红色显示锁状态非常直观
            if (_model.VisualLockCount.Value > 0)
                 GUILayout.Label($"<b><color=red>系统锁状态: {_model.VisualLockCount.Value} (动画播放阻塞中!)</color></b>");
            else
                 GUILayout.Label($"<b><color=green>系统锁状态: 0 (待命闲置中)</color></b>");
            
            GUILayout.Space(10);
            GUILayout.Label($"<b>--- 怪物状态 ---</b>");
            foreach(var enemy in _model.Enemies)
            {
                GUILayout.Label($"[{enemy.Name}] HP: {enemy.CurrentHp.Value}/{enemy.MaxHp.Value} | 护甲: {enemy.Block.Value}");
                foreach(var buff in enemy.Buffs)
                    GUILayout.Label($"    -> 取向Buff: {buff.Id}: {buff.Value}层");
            }

            GUILayout.Space(10);
            GUILayout.Label($"<b>--- 玩家状态 ---</b>");
            GUILayout.Label($"[{_model.Player.Name}] HP: {_model.Player.CurrentHp.Value}/{_model.Player.MaxHp.Value} | 护甲: {_model.Player.Block.Value}");
            GUILayout.Label($"<b>能量: <color=#FFD700>{_model.Player.Energy.Value}/{_model.Player.MaxEnergy.Value}</color></b>");
            foreach (var buff in _model.Player.Buffs)
                GUILayout.Label($"    -> {buff.Id}: {buff.Value}层");

            GUILayout.Space(20);
            GUILayout.Label($"<b>--- 战场区域 ---</b>");
            GUILayout.BeginHorizontal();
            GUILayout.Label($"抽牌堆: <color=cyan>{_model.DrawPile.Count}</color>");
            GUILayout.Label($"弃牌堆: <color=gray>{_model.DiscardPile.Count}</color>");
            GUILayout.Label($"消耗堆: {_model.ExhaustPile.Count}");
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label($"<b>--- 手牌区 (点击打出) ---</b>");
            
            // 系统正在播动画时，彻底锁死所有按钮
            GUI.enabled = _model.VisualLockCount.Value == 0; 
            
            var handCopy = new List<CardData>(_model.Hand); 
            foreach(var card in handCopy)
            {
                // 若能量不足，把按钮变灰
                bool canAfford = _model.Player.Energy.Value >= card.CurrentCost.Value;
                GUI.enabled = canAfford && _model.VisualLockCount.Value == 0;

                string color = canAfford ? "white" : "red";
                string shortId = card.InstanceId.Substring(0, 4);
                if (GUILayout.Button($"<b>{card.Name}</b> [#{shortId}]\n{card.Description.Value}", GUILayout.Height(60)))
                {
                    PlayCard(card);
                }
                GUI.enabled = _model.VisualLockCount.Value == 0; 
            }

            GUILayout.Space(15);
            GUILayout.Label("<b>--- 战斗日志 ---</b>");
            foreach(var log in _battleLogs) GUILayout.Label($"<size=11>{log}</size>");

            GUILayout.Space(20);
            if (GUILayout.Button("【结束回合】", GUILayout.Height(50)))
            {
                EndTurn();
            }

            GUI.enabled = true; // 恢复
            GUILayout.EndArea();
        }
        #endregion
    }
}
