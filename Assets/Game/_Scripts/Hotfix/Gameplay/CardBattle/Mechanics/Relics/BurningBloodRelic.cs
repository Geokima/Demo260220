using Framework;
using UnityEngine;

namespace Game.Gameplay.CardBattle
{
    /// <summary> 遗物逻辑处理器 </summary>
    public class BurningBloodRelic : IBattleRelic, IController
    {
        public IArchitecture Architecture { get; set; }
        public IArchitecture GetArchitecture() => Architecture ??= CardBattleArchitecture.Instance;
        private IUnregister _unregister;

        public void Init(ISystem service)
        {
            _unregister = this.RegisterEvent<BattleEndEvent>(OnBattleEnd);
        }

        public void Deinit()
        {
            _unregister?.Unregister();
            _unregister = null;
        }

        private void OnBattleEnd(BattleEndEvent e)
        {
            if (e.IsWin)
            {
                Debug.Log("[BurningBloodRelic] 触发燃烧之血！战斗胜利，回复 6 点 HP。");
                var queue = this.GetSystem<IActionQueueService>();
                var model = this.GetModel<BattleModel>();

                // 商业级：通过 ActionPool 产生动作
                queue.Enqueue(ActionPool<HealAction>.Allocate().Init(model.Player, 6));
            }
        }
    }
}
