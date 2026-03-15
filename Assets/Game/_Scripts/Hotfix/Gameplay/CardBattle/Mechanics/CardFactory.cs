using System.Collections.Generic;
using Game.Config;

namespace Game.Gameplay.CardBattle
{
    /// <summary> 
    /// [Logic] 卡牌工厂：解析静态配置档 (CardConfig)，映射生成运行时的对象池逻辑动作 (IBattleAction)
    /// </summary>
    public static class CardFactory
    {
        public static List<IBattleAction> CreateActions(CardConfig config, CardBattleContext context)
        {
            var actions = new List<IBattleAction>();

            // 解析配置表中配置的【效果组】
            foreach (var effect in config.Effects)
            {
                // 对于“抽牌”等只对自己或系统级生效而不受目标选择影响的动作，独立入队
                if (effect.Type == "DrawCard")
                {
                    actions.Add(ActionPool<DrawCardAction>.Allocate().Init(effect.Value));
                    continue;
                }

                // 对于每一个选中的攻击目标（单体就1个，AOE就全体）
                foreach (var target in context.Targets)
                {
                    switch (effect.Type)
                    {
                        case "Damage":
                            actions.Add(ActionPool<DamageAction>.Allocate().Init(context.Source, target, effect.Value));
                            break;
                        case "Block":
                            // 护甲通常对自己生效目标（此时上下文 Target 应为 Player），由上下文构建器决定
                            actions.Add(ActionPool<BlockAction>.Allocate().Init(target, effect.Value));
                            break;
                        case "ApplyBuff":
                            actions.Add(ActionPool<ApplyBuffAction>.Allocate().Init(target, effect.StringId, effect.Value));
                            break;
                        case "Heal":
                            actions.Add(ActionPool<HealAction>.Allocate().Init(target, effect.Value));
                            break;
                    }
                }
            }

            return actions;
        }
    }
}
