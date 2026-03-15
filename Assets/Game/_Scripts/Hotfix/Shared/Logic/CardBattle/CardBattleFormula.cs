using System;
using System.Collections.Generic;
using System.Linq;
using Game.Shared.Consts.CardBattle;
using Game.Shared.DTOs.CardBattle;

namespace Game.Shared.Logic.CardBattle
{
    /// <summary>
    /// (Pure Logic) 战?公式库 - 严禁引用 UnityEngine 或 System
    /// 确保前后端计算结果绝对一致?
    /// </summary>
    public static class CardBattleFormula
    {
        /// <summary>
        /// 计算最终伤?
        /// </summary>
        /// <param name="baseDamage">基础伤害</param>
        /// <param name="sourceBuffs">攻击者的 Buff 列表</param>
        /// <param name="targetBuffs">受击者的 Buff 列表</param>
        /// <returns>最终结算数值</returns>
        public static int CalculateDamage(int baseDamage, List<BuffDto> sourceBuffs, List<BuffDto> targetBuffs)
        {
            float result = baseDamage;

            // 1. 力量增益 (Strength)
            var strength = GetValue(sourceBuffs, BuffIDs.Strength);
            result += strength;

            // 2. 来源虚? (Weak)
            if (HasBuff(sourceBuffs, BuffIDs.Weak))
            {
                result *= 0.75f;
            }

            // 3. 目标易? (Vulnerable)
            if (HasBuff(targetBuffs, BuffIDs.Vulnerable))
            {
                result *= 1.5f;
            }

            return Math.Max(0, (int)Math.Floor(result));
        }

        /// <summary>
        /// 计算最终护?
        /// </summary>
        public static int CalculateBlock(int baseBlock, List<BuffDto> entityBuffs)
        {
            float result = baseBlock;
            
            // 敏捷增益 (Dexterity)
            var dexterity = GetValue(entityBuffs, BuffIDs.Dexterity);
            result += dexterity;

            return Math.Max(0, (int)Math.Floor(result));
        }

        private static int GetValue(List<BuffDto> buffs, string buffId)
        {
            if (buffs == null) return 0;
            return buffs.FirstOrDefault(b => b.Id == buffId)?.Value ?? 0;
        }

        private static bool HasBuff(List<BuffDto> buffs, string buffId)
        {
            if (buffs == null) return false;
            return buffs.Any(b => b.Id == buffId && b.Value > 0);
        }
    }
}
