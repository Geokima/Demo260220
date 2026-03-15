using Framework;
using System.Collections.Generic;
using System.Linq;
using Game.Shared.Consts.CardBattle;
using Game.Shared.DTOs.CardBattle;

namespace Game.Gameplay.CardBattle
{
    public interface IBuffService : ISystem
    {
        void ApplyBuff(EntityData target, string buffId, int amount);
        int GetBuffStack(EntityData target, string buffId);
        void ProcessTurnEnd(EntityData target);
    }

    /// <summary> Buff 业务管理 </summary>
    public class BuffService : AbstractSystem, IBuffService
    {
        public override void Init() { }

        public void ApplyBuff(EntityData target, string buffId, int amount)
        {
            var existing = target.Buffs.FirstOrDefault(b => b.Id == buffId);
            if (existing != null)
            {
                existing.Value += amount;
            }
            else
            {
                target.Buffs.Add(new BuffDto { Id = buffId, Value = amount });
            }
        }

        public int GetBuffStack(EntityData target, string buffId)
        {

            return target.Buffs.FirstOrDefault(b => b.Id == buffId)?.Value ?? 0;
        }

        public void ProcessTurnEnd(EntityData target)
        {

            var vulnerable = target.Buffs.FirstOrDefault(b => b.Id == BuffIDs.Vulnerable);
            if (vulnerable != null && vulnerable.Value > 0)
            {
                vulnerable.Value--;
                if (vulnerable.Value <= 0)
                    target.Buffs.Remove(vulnerable);
            }
            
            var weak = target.Buffs.FirstOrDefault(b => b.Id == BuffIDs.Weak);
            if (weak != null && weak.Value > 0)
            {
                weak.Value--;
                if (weak.Value <= 0)
                    target.Buffs.Remove(weak);
            }
        }
    }
}
