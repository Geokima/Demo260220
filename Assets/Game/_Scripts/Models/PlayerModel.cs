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
        public BindableProperty<int> Gold { get; } = new BindableProperty<int>(0);
        /// <summary>钻石</summary>
        public BindableProperty<int> Diamond { get; } = new BindableProperty<int>(0);
        /// <summary>经验</summary>
        public BindableProperty<int> Exp { get; } = new BindableProperty<int>(0);
        /// <summary>体力</summary>
        public BindableProperty<int> Energy { get; } = new BindableProperty<int>(100);

        /// <summary>等级（计算属性，由经验自动计算）</summary>
        private BindableProperty<int> _level = new BindableProperty<int>(1);
        public IReadonlyBindableProperty<int> Level => _level;

        /// <summary>基础体力上限</summary>
        public const int BaseMaxEnergy = 100;

        /// <summary>每级增加的体力上限</summary>
        public const int EnergyPerLevel = 10;

        /// <summary>体力恢复间隔（秒）</summary>
        public const int EnergyRecoverInterval = 10; // 10秒恢复1点

        /// <summary>上次体力恢复时间</summary>
        public long LastEnergyRecoverTime { get; set; } = 0;
        
        /// <summary>经验需求表（每级所需经验）</summary>
        private static readonly int[] ExpTable = new int[]
        {
            8,      // 1级
            22,     // 2级
            45,     // 3级
            76,     // 4级
            116,    // 5级
            165,    // 6级
            222,    // 7级
            288,    // 8级
            362,    // 9级
            445,    // 10级
            537,    // 11级
            637,    // 12级
            746,    // 13级
            864,    // 14级
            990,    // 15级
            1125,   // 16级
            1269,   // 17级
            1421,   // 18级
            1582,   // 19级
            1752,   // 20级
            1930,   // 21级
            2117,   // 22级
            2313,   // 23级
            2517,   // 24级
            2730,   // 25级
            2952,   // 26级
            3182,   // 27级
            3421,   // 28级
            3669,   // 29级
            3925,   // 30级
            4190,   // 31级
            4464,   // 32级
            4746,   // 33级
            5037,   // 34级
            5337,   // 35级
            5645,   // 36级
            5962,   // 37级
            6288,   // 38级
            6622,   // 39级
            6965,   // 40级
            7317,   // 41级
            7677,   // 42级
            8046,   // 43级
            8424,   // 44级
            8810,   // 45级
            9205,   // 46级
            9609,   // 47级
            10021,  // 48级
            10442,  // 49级
            10872,  // 50级
            11310,  // 51级
            11757,  // 52级
            12213,  // 53级
            12677,  // 54级
            13150,  // 55级
            13632,  // 56级
            14122,  // 57级
            14621,  // 58级
            15129,  // 59级
            15645,  // 60级
            16170,  // 61级
            16704,  // 62级
            17246,  // 63级
            17797,  // 64级
            18357,  // 65级
            18925,  // 66级
            19502,  // 67级
            20088,  // 68级
            20682,  // 69级
            21285,  // 70级
            21897,  // 71级
            22517,  // 72级
            23146,  // 73级
            23784,  // 74级
            24430,  // 75级
            25085,  // 76级
            25749,  // 77级
            26421,  // 78级
            27102,  // 79级
            27792,  // 80级
            28490,  // 81级
            29197,  // 82级
            29913,  // 83级
            30637,  // 84级
            31370,  // 85级
            32112,  // 86级
            32862,  // 87级
            33621,  // 88级
            34389,  // 89级
            35165,  // 90级
            35950,  // 91级
            36744,  // 92级
            37546,  // 93级
            38357,  // 94级
            39177,  // 95级
            40005,  // 96级
            40842,  // 97级
            41688,  // 98级
            42542,  // 99级
            43405,  // 100级
        };
        
        /// <summary>
        /// 获取当前最大体力（随等级提升）
        /// </summary>
        public int GetMaxEnergy()
        {
            return BaseMaxEnergy + (Level.Value - 1) * EnergyPerLevel;
        }

        /// <summary>
        /// 获取资源
        /// </summary>
        public int GetResource(ResourceType type)
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
        public void SetResource(ResourceType type, int amount)
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
        public void AddResource(ResourceType type, int amount)
        {
            if (amount <= 0) return;
            SetResource(type, GetResource(type) + amount);
        }

        /// <summary>
        /// 消耗资源
        /// </summary>
        public bool ConsumeResource(ResourceType type, int amount)
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
        public bool HasEnough(ResourceType type, int amount)
        {
            return GetResource(type) >= amount;
        }

        /// <summary>
        /// 根据经验计算等级，最高100级
        /// </summary>
        public int CalculateLevel(int exp)
        {
            for (int i = ExpTable.Length - 1; i >= 0; i--)
            {
                if (exp >= ExpTable[i])
                    return Math.Min(i + 1, 100);
            }
            return 1;
        }
        
        /// <summary>
        /// 获取升级所需经验
        /// </summary>
        public int GetExpForNextLevel(int level)
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
            int maxEnergy = GetMaxEnergy();
            if (Energy.Value >= maxEnergy) return;

            long elapsed = currentTime - LastEnergyRecoverTime;
            if (elapsed < EnergyRecoverInterval) return;

            int recoverPoints = (int)(elapsed / EnergyRecoverInterval);
            int newEnergy = Math.Min(Energy.Value + recoverPoints, maxEnergy);

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
