using Framework;
using System;

namespace Game.Player
{
    public class PlayerModel : AbstractModel
    {
        public enum ResourceType
        {
            Gold,
            Diamond,
            Exp,
            Energy,
        }

        public BindableProperty<int> Gold { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> Diamond { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> Exp { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> Energy { get; } = new BindableProperty<int>(100);

        private BindableProperty<int> _level = new BindableProperty<int>(1);
        public IReadonlyBindableProperty<int> Level => _level;

        public const int BaseMaxEnergy = 100;
        public const int EnergyPerLevel = 10;
        public const int EnergyRecoverInterval = 10;

        public long LastEnergyRecoverTime { get; set; }
        public long ServerTime { get; set; }

        public int GetMaxEnergy()
        {
            return BaseMaxEnergy + (_level.Value - 1) * EnergyPerLevel;
        }

        private static readonly int[] ExpTable = {
            8, 30, 75, 151, 267, 432, 654, 942, 1304, 1749,
            2286, 2923, 3669, 4533, 5523, 6648, 7917, 9338, 10920, 12672,
            14602, 16719, 19032, 21549, 24279, 27231, 30413, 33834, 37503, 41428,
            45618, 50082, 54828, 59865, 65202, 70847, 76809, 83097, 89719, 96684,
            104001, 111678, 119724, 128148, 136958, 146163, 155772, 165793, 176235, 187107,
            198417, 210174, 222387, 235064, 248214, 261846, 275968, 290589, 305718, 321363,
            337533, 354237, 371483, 389280, 407637, 426562, 446064, 466152, 486834, 508119,
            530016, 552533, 575679, 599463, 623893, 648978, 674727, 701148, 728250, 756042,
            784532, 813729, 843642, 874279, 905649, 937761, 970623, 1004244, 1038633, 1073798,
            1109748, 1146492, 1184038, 1222395, 1261572, 1301577, 1342419, 1384107, 1426649, 1470054
        };

        public int CalculateLevel(int exp)
        {
            int left = 0, right = ExpTable.Length - 1;
            while (left <= right)
            {
                int mid = (left + right) / 2;
                if (ExpTable[mid] <= exp)
                    left = mid + 1;
                else
                    right = mid - 1;
            }
            return Math.Min(left + 1, 100);
        }

        public (int curExp, int maxExp) GetLevelExpProgress(int totalExp)
        {
            int level = CalculateLevel(totalExp);
            int curLevelExp = level > 1 ? ExpTable[level - 2] : 0;
            int nextLevelExp = level < 100 ? ExpTable[level - 1] : curLevelExp;
            return (totalExp - curLevelExp, nextLevelExp - curLevelExp);
        }

        public int GetCurrentEnergy()
        {
            if (ServerTime == 0 || LastEnergyRecoverTime == 0)
                return Energy.Value;

            long elapsed = ServerTime - LastEnergyRecoverTime;
            if (elapsed < EnergyRecoverInterval)
                return Energy.Value;

            int recovered = (int)(elapsed / EnergyRecoverInterval);
            int maxEnergy = GetMaxEnergy();
            int newEnergy = Math.Min(Energy.Value + recovered, maxEnergy);
            return newEnergy;
        }

        public long GetEnergyRecoverCountdown()
        {
            if (ServerTime == 0 || LastEnergyRecoverTime == 0)
                return 0;

            int currentEnergy = GetCurrentEnergy();
            if (currentEnergy >= GetMaxEnergy())
                return 0;

            long nextRecover = LastEnergyRecoverTime + ((currentEnergy - Energy.Value + 1) * EnergyRecoverInterval);
            return nextRecover - ServerTime;
        }

        public void Clear()
        {
            Gold.Value = 0;
            Diamond.Value = 0;
            Exp.Value = 0;
            Energy.Value = 100;
            _level.Value = 1;
            LastEnergyRecoverTime = 0;
            ServerTime = 0;
        }
    }
}
