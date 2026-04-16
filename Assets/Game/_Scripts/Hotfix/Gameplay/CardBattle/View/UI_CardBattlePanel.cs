using UnityEngine;
using Framework;
using Framework.Modules.UI;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace Game.Gameplay.CardBattle
{
    /// <summary>
    /// 正式的卡牌战斗UI面板
    /// 遵循 QFramework + 响应式 Model 绑定
    /// </summary>
    public partial class UI_CardBattlePanel : UIPanel, IController
    {
        private IArchitecture _battleArch;

        private BattleModel _model;
        private ITurnService _turnService;
        private IActionQueueService _queue;
        
        // 保存绑定引用用于清理
        private List<IUnregister> _unregisters = new List<IUnregister>();

        public override void OnOpen(object data = null)
        {
            // 0. 架构注入判定
            _battleArch = data as IArchitecture ?? CardBattleArchitecture.Instance;

            // 重要：确保架构已启动（执行 RegisterModule）
            if (_battleArch is CardBattleArchitecture)
            {
                CardBattleArchitecture.Launch();
            }

            _model = _battleArch.GetModel<BattleModel>();
            
            // 安全检查：如果 Model 没获取到，说明架构配置有问题
            if (_model == null)
            {
                Debug.LogError("[UI] BattleModel is null! Ensure Architecture is launched properly.");
                return;
            }

            // 安全检查：如果 Player 是空的（比如测试时没初始化），补一个默认数据防止报错
            if (_model.Player == null)
            {
                _model.Player = new EntityData("P1", EntityType.Player, "测试勇者", 100);
            }

            _turnService = _battleArch.GetSystem<ITurnService>();
            _queue = _battleArch.GetSystem<IActionQueueService>();

            // 1. 绑定基础数值 (响应式)
            _unregisters.Add(_model.TurnCount.RegisterWithInitValue(count => Text_TurnCount.text = $"第 {count} 回合"));
            _unregisters.Add(_model.VisualLockCount.RegisterWithInitValue(lockCount => Obj_VisualLock.SetActive(lockCount > 0)));

            // 2. 玩家状态绑定
            _unregisters.Add(_model.Player.CurrentHp.RegisterWithInitValue(hp => UpdatePlayerHp()));
            _unregisters.Add(_model.Player.Energy.RegisterWithInitValue(energy => Text_PlayerEnergy.text = $"{energy}/{_model.Player.MaxEnergy.Value}"));
            _unregisters.Add(_model.Player.Block.RegisterWithInitValue(block => Text_PlayerBlock.text = block > 0 ? $"[{block}]" : ""));

            // 3. 堆栈计数监听
            _unregisters.Add(_model.DrawPile.OnCountChanged.Register(_ => UpdateDrawPileCount()));
            _unregisters.Add(_model.DiscardPile.OnCountChanged.Register(_ => UpdateDiscardPileCount()));
            UpdateDrawPileCount();
            UpdateDiscardPileCount();

            // 4. 事件监听与手牌响应
            Btn_EndTurn.onClick.AddListener(OnEndTurnBtnClick);
            
            // 响应手牌变化
            _unregisters.Add(_model.Hand.OnAdd.Register((i, card) => RefreshHand()));
            _unregisters.Add(_model.Hand.OnRemove.Register((i, card) => RefreshHand()));
            _unregisters.Add(_model.Hand.OnClear.Register(() => RefreshHand()));
            
            // 5. 初始同步
            RefreshHand();
            InitExistingEnemies(); // 适配你手动摆放在场景里的怪物
            
            // 6. 开启队列轮询处理
            _queue.ProcessQueueAsync().Forget();
            
            // 6. 监听选中状态实现指向逻辑
            _unregisters.Add(_model.SelectedCard.Register(OnSelectedCardChanged));
        }

        [Header("Prefabs")]
        public GameObject CardPrefab;

        private List<UI_CardItem> _cardItems = new List<UI_CardItem>();


        private void InitExistingEnemies()
        {
            // 如果场景里已经摆好了怪物，我们把它们和数据关联起来
            var enemiesInScene = EnemyContainer.GetComponentsInChildren<UI_EnemyItem>();
            for (int i = 0; i < enemiesInScene.Length; i++)
            {
                if (i < _model.Enemies.Count)
                {
                    enemiesInScene[i].SetData(_model.Enemies[i], _battleArch);
                    Debug.Log($"[UI] 已激活场景中的怪物对象: {enemiesInScene[i].gameObject.name} -> {_model.Enemies[i].Name}");
                }
            }
        }

        private void RefreshHand()
        {
            // 清理旧手牌
            foreach (var item in _cardItems) if(item != null) Destroy(item.gameObject);
            _cardItems.Clear();

            // 生成新手牌
            foreach (var cardData in _model.Hand)
            {
                var go = Instantiate(CardPrefab, HandContainer);
                var item = go.GetComponent<UI_CardItem>();
                
                // 【核心注入】由上级面板注入架构给子物体
                item.Architecture = _battleArch;
                
                item.SetData(cardData);
                _cardItems.Add(item);
            }
        }

        private void OnSelectedCardChanged(CardData card)
        {
            if (card != null)
            {
                Debug.Log($"<color=orange>开始指向交互：{card.Name}</color>");
                // 这里可以激活箭头组件 (TargetArrow.Show())
            }
            else
            {
                Debug.Log("交互结束");
                // 箭头隐藏 (TargetArrow.Hide())
            }
        }

        // Update 移除，逻辑已平移至 UI_CardItem 的 Drag 系统中

        private void UpdatePlayerHp()
        {
            Text_PlayerHP.text = $"{_model.Player.CurrentHp.Value}/{_model.Player.MaxHp.Value}";
        }

        private void UpdateDrawPileCount()
        {
            Text_DrawCount.text = _model.DrawPile.Count.ToString();
        }

        private void UpdateDiscardPileCount()
        {
            Text_DiscardCount.text = _model.DiscardPile.Count.ToString();
        }

        private void OnEndTurnBtnClick()
        {
            if (_model.VisualLockCount.Value > 0) return;
            
            _turnService.EndPlayerTurn();
            _queue.ProcessQueueAsync().Forget();
        }

        public override void OnClose()
        {
            // 面板关闭时的清理
            
            // 清理事件监听
            Btn_EndTurn.onClick.RemoveListener(OnEndTurnBtnClick);
            
            // 清理绑定
            foreach (var unregister in _unregisters)
            {
                unregister.Unregister();
            }
            _unregisters.Clear();
        }
    }
}
