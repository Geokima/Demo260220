using System.Collections.Generic;
using Framework;
using Game.Shared.DTOs.CardBattle;

namespace Game.Gameplay.CardBattle
{
    public enum EntityType
    {
        Player,
        Enemy
    }

    /// <summary>
    /// 战?实体数据 (主角 / 怪物)
    /// </summary>
    public class EntityData
    {
        public string Id { get; set; }
        public EntityType Type { get; set; }
        public string Name { get; set; }

        public BindableProperty<int> MaxHp { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> CurrentHp { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> Block { get; } = new BindableProperty<int>(0);
        
        // 通常只有玩家有能量，怪物不需要，但也可能用能量设计特殊怪物
        public BindableProperty<int> Energy { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> MaxEnergy { get; } = new BindableProperty<int>(3);

        /// <summary>
        /// 实体持有的 Buff 列表 (数据层)
        /// 商业级规?：直接持有 Dto 格式以便于 Formula 速配计算
        /// </summary>
        public List<BuffDto> Buffs { get; } = new List<BuffDto>();

        public EntityData(string id, EntityType type, string name, int maxHp)
        {
            Id = id;
            Type = type;
            Name = name;
            MaxHp.Value = maxHp;
            CurrentHp.Value = maxHp;
            Block.Value = 0;
        }

        public EntityDto ToDto()
        {
            return new EntityDto
            {
                InstanceId = Id.GetHashCode(), // 仿真 ID
                Hp = CurrentHp.Value,
                MaxHp = MaxHp.Value,
                Block = Block.Value,
                Buffs = new List<BuffDto>(Buffs)
            };
        }
    }
}
