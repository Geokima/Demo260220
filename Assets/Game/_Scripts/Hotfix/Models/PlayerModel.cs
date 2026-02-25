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
        
        /// <summary>经验需求表（升到该等级所需的总经验）</summary>
        private static readonly int[] ExpTable = new int[]
        {
            8,      // 1级 (8)
            30,     // 2级 (8+22)
            75,     // 3级 (30+45)
            151,    // 4级 (75+76)
            267,    // 5级 (151+116)
            432,    // 6级 (267+165)
            654,    // 7级 (432+222)
            942,    // 8级 (654+288)
            1304,   // 9级 (942+362)
            1749,   // 10级 (1304+445)
            2286,   // 11级 (1749+537)
            2923,   // 12级 (2286+637)
            3669,   // 13级 (2923+746)
            4533,   // 14级 (3669+864)
            5523,   // 15级 (4533+990)
            6648,   // 16级 (5523+1125)
            7917,   // 17级 (6648+1269)
            9338,   // 18级 (7917+1421)
            10920,  // 19级 (9338+1582)
            12672,  // 20级 (10920+1752)
            14602,  // 21级 (12672+1930)
            16719,  // 22级 (14602+2117)
            19032,  // 23级 (16719+2313)
            21549,  // 24级 (19032+2517)
            24279,  // 25级 (21549+2730)
            27231,  // 26级 (24279+2952)
            30413,  // 27级 (27231+3182)
            33834,  // 28级 (30413+3421)
            37503,  // 29级 (33834+3669)
            41428,  // 30级 (37503+3925)
            45618,  // 31级 (41428+4190)
            50082,  // 32级 (45618+4464)
            54828,  // 33级 (50082+4746)
            59865,  // 34级 (54828+5037)
            65202,  // 35级 (59865+5337)
            70847,  // 36级 (65202+5645)
            76809,  // 37级 (70847+5962)
            83097,  // 38级 (76809+6288)
            89719,  // 39级 (83097+6622)
            96684,  // 40级 (89719+6965)
            104001, // 41级 (96684+7317)
            111678, // 42级 (104001+7677)
            119724, // 43级 (111678+8046)
            128148, // 44级 (119724+8424)
            136958, // 45级 (128148+8810)
            146163, // 46级 (136958+9205)
            155772, // 47级 (146163+9609)
            165793, // 48级 (155772+10021)
            176235, // 49级 (165793+10442)
            187107, // 50级 (176235+10872)
            198417, // 51级 (187107+11310)
            210174, // 52级 (198417+11757)
            222387, // 53级 (210174+12213)
            235064, // 54级 (222387+12677)
            248214, // 55级 (235064+13150)
            261846, // 56级 (248214+13632)
            275968, // 57级 (261846+14122)
            290589, // 58级 (275968+14621)
            305718, // 59级 (290589+15129)
            321363, // 60级 (305718+15645)
            337533, // 61级 (321363+16170)
            354237, // 62级 (337533+16704)
            371483, // 63级 (354237+17246)
            389280, // 64级 (371483+17797)
            407637, // 65级 (389280+18357)
            426562, // 66级 (407637+18925)
            446064, // 67级 (426562+19502)
            466152, // 68级 (446064+20088)
            486834, // 69级 (466152+20682)
            508119, // 70级 (486834+21285)
            530016, // 71级 (508119+21897)
            552533, // 72级 (530016+22517)
            575679, // 73级 (552533+23146)
            599463, // 74级 (575679+23784)
            623893, // 75级 (599463+24430)
            648978, // 76级 (623893+25085)
            674727, // 77级 (648978+25749)
            701148, // 78级 (674727+26421)
            728250, // 79级 (701148+27102)
            756042, // 80级 (728250+27792)
            784532, // 81级 (756042+28490)
            813729, // 82级 (784532+29197)
            843642, // 83级 (813729+29913)
            874279, // 84级 (843642+30637)
            905649, // 85级 (874279+31370)
            937761, // 86级 (905649+32112)
            970623, // 87级 (937761+32862)
            1004244,// 88级 (970623+33621)
            1038633,// 89级 (1004244+34389)
            1073798,// 90级 (1038633+35165)
            1109748,// 91级 (1073798+35950)
            1146492,// 92级 (1109748+36744)
            1184038,// 93级 (1146492+37546)
            1222395,// 94级 (1184038+38357)
            1261572,// 95级 (1222395+39177)
            1301577,// 96级 (1261572+40005)
            1342419,// 97级 (1301577+40842)
            1384107,// 98级 (1342419+41688)
            1426649,// 99级 (1384107+42542)
            1470054,// 100级 (1426649+43405)
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
        /// 根据经验计算等级，最高100级（使用二分查找）
        /// </summary>
        public int CalculateLevel(int exp)
        {
            // ExpTable存储的是升到该等级所需的总经验
            // 使用二分查找找到第一个大于exp的位置
            int left = 0, right = ExpTable.Length - 1;
            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                if (ExpTable[mid] <= exp)
                    left = mid + 1;
                else
                    right = mid - 1;
            }
            return Math.Min(left + 1, 100);
        }
        
        /// <summary>
        /// 获取当前等级的经验进度（当前等级已拥有的经验 / 当前等级升级所需经验）
        /// </summary>
        public (int current, int max) GetLevelExpProgress(int totalExp)
        {
            int level = CalculateLevel(totalExp);
            
            if (level <= 1)
            {
                // 1级：当前进度就是总经验，升级需要ExpTable[0]
                return (totalExp, ExpTable[0]);
            }
            
            int prevLevelTotalExp = ExpTable[level - 2];    // 升到上一级的总经验
            int currentLevelTotalExp = ExpTable[level - 1]; // 升到当前级的总经验
            
            int current = totalExp - prevLevelTotalExp;                 // 当前等级进度
            int max = currentLevelTotalExp - prevLevelTotalExp;         // 当前等级升级所需经验
            
            return (current, max);
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
