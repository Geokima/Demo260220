using Framework;
using UnityEngine;
using Game.Shared.Logic.CardBattle;

namespace Game.Gameplay.CardBattle
{
    public interface INumericalService : ISystem
    {
        int CalculateDamage(EntityData source, EntityData target, int baseDamage);
        int CalculateBlock(EntityData source, int baseBlock);
    }

    public class NumericalService : AbstractSystem, INumericalService
    {
        public override void Init() { }

        public int CalculateDamage(EntityData source, EntityData target, int baseDamage)
        {
            // 允许 source 或 target 为空（例如预览描述时），Formula 内部已处理 null
            return CardBattleFormula.CalculateDamage(baseDamage, source?.Buffs, target?.Buffs);
        }

        public int CalculateBlock(EntityData source, int baseBlock)
        {
            return CardBattleFormula.CalculateBlock(baseBlock, source?.Buffs);
        }
    }
}
