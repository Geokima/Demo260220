using Framework;
using System;

namespace Game.Models
{
    /// <summary>
    /// 玩家数据模型 - 包含货币、等级、经验、体力等基础属性
    /// </summary>
    public class PlayerModel : AbstractModel
    {
        /// <summary>
        /// 资源类型
        /// </summary>
        public enum ResourceType
        {
            Gold,       // 金币
            Diamond,    // 钻石
            Exp,        // 经验
            Energy,     // 体力
        }

        /// <summary>金币</summary>
        public BindableProperty<long> Gold { get; } = new BindableProperty<long>(0);
        /// <summary>钻石</summary>
        public BindableProperty<long> Diamond { get; } = new BindableProperty<long>(0);
        /// <summary>经验</summary>
        public BindableProperty<long> Exp { get; } = new BindableProperty<long>(0);
        /// <summary>体力</summary>
        public BindableProperty<long> Energy { get; } = new BindableProperty<long>(100);
        
        /// <summary>等级（计算属性，由经验自动计算）</summary>
        private BindableProperty<int> _level = new BindableProperty<int>(1);
        public IReadonlyBindableProperty<int> Level => _level;
        
        /// <summary>基础体力上限</summary>
        public const long BaseMaxEnergy = 100;
        
        /// <summary>每级增加的体力上限</summary>
        public const long EnergyPerLevel = 10;
        
        /// <summary>体力恢复间隔（秒）</summary>
        public const int EnergyRecoverInterval = 10; // 10秒恢复1点
        
        /// <summary>上次体力恢复时间</summary>
        public long LastEnergyRecoverTime { get; set; } = 0;
        
        /// <summary>经验需求表（每级所需经验）</summary>
        private static readonly long[] ExpTable = new long[]
        {
            0,      // 1级
            100,    // 2级
            300,    // 3级
            600,    // 4级
            1000,   // 5级
            1500,   // 6级
            2100,   // 7级
            2800,   // 8级
            3600,   // 9级
            4500,   // 10级
        };
        
        /// <summary>
        /// 获取当前最大体力（随等级提升）
        /// </summary>
        public long GetMaxEnergy()
        {
            return BaseMaxEnergy + (Level.Value - 1) * EnergyPerLevel;
        }

        /// <summary>
        /// 获取资源
        /// </summary>
        public long GetResource(ResourceType type)
        {
            return type switch
            {
                ResourceType.Gold => Gold.Value,
                ResourceType.Diamond => Diamond.Value,
                ResourceType.Exp => Exp.Value,
                ResourceType.Energy => Energy.Value,
                _ => 0
            };
        }

        /// <summary>
        /// 设置资源
        /// </summary>
        public void SetResource(ResourceType type, long amount)
        {
            switch (type)
            {
                case ResourceType.Gold: Gold.Value = amount; break;
                case ResourceType.Diamond: Diamond.Value = amount; break;
                case ResourceType.Exp: Exp.Value = amount; break;
                case ResourceType.Energy: Energy.Value = amount; break;
            }
        }

        /// <summary>
        /// 增加资源
        /// </summary>
        public void AddResource(ResourceType type, long amount)
        {
            if (amount <= 0) return;
            SetResource(type, GetResource(type) + amount);
        }

        /// <summary>
        /// 消耗资源
        /// </summary>
        public bool ConsumeResource(ResourceType type, long amount)
        {
            if (amount <= 0) return true;
            var current = GetResource(type);
            if (current < amount) return false;
            SetResource(type, current - amount);
            return true;
        }

        /// <summary>
        /// 检查资源是否足够
        /// </summary>
        public bool HasEnough(ResourceType type, long amount)
        {
            return GetResource(type) >= amount;
        }

        /// <summary>
        /// 根据经验计算等级
        /// </summary>
        public int CalculateLevel(long exp)
        {
            for (int i = ExpTable.Length - 1; i >= 0; i--)
            {
                if (exp >= ExpTable[i])
                    return i + 1;
            }
            return 1;
        }
        
        /// <summary>
        /// 获取升级所需经验
        /// </summary>
        public long GetExpForNextLevel(int level)
        {
            if (level >= ExpTable.Length)
                return ExpTable[ExpTable.Length - 1] * 2; // 满级后翻倍
            return ExpTable[level];
        }
        
        /// <summary>
        /// 更新等级（经验变化时调用）
        /// </summary>
        private void UpdateLevel()
        {
            int newLevel = CalculateLevel(Exp.Value);
            if (newLevel != _level.Value)
            {
                int oldLevel = _level.Value;
                _level.Value = newLevel;
                // 可以在这里触发升级事件
                // this.SendEvent(new LevelUpEvent { OldLevel = oldLevel, NewLevel = newLevel });
            }
        }
        
        /// <summary>
        /// 恢复体力（根据离线时间计算）
        /// </summary>
        public void RecoverEnergy(long currentTime)
        {
            long maxEnergy = GetMaxEnergy();
            if (Energy.Value >= maxEnergy) return;
            
            long elapsed = currentTime - LastEnergyRecoverTime;
            if (elapsed < EnergyRecoverInterval) return;
            
            long recoverPoints = elapsed / EnergyRecoverInterval;
            long newEnergy = Math.Min(Energy.Value + recoverPoints, maxEnergy);
            
            Energy.Value = newEnergy;
            LastEnergyRecoverTime = currentTime;
        }

        public override void Init()
        {
            // 经验变化时自动更新等级
            Exp.Register(_ => UpdateLevel());
        }

        public override void Deinit()
        {
            Gold.Value = 0;
            Diamond.Value = 0;
            Exp.Value = 0;
            Energy.Value = 100;
            _level.Value = 1;
            LastEnergyRecoverTime = 0;
        }

        /// <summary>
        /// 清除所有数据（退出登录时调用）
        /// </summary>
        public void Clear()
        {
            Gold.Value = 0;
            Diamond.Value = 0;
            Exp.Value = 0;
            Energy.Value = 100;
            _level.Value = 1;
            LastEnergyRecoverTime = 0;
        }
    }
}
