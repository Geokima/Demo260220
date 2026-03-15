using Framework.Modules.Config;
using System.Collections.Generic;

namespace Game.Config
{
    public class CardEffectConfig
    {
        public string Type { get; set; } // "Damage", "Block", "ApplyBuff", "Heal", "DrawCard"
        public int Value { get; set; }
        public string StringId { get; set; } // For buff ID or specific logic string
    }

    public class CardConfig : IConfigRow
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Desc { get; set; }
        public int Cost { get; set; }
        public string TargetType { get; set; } // "Self", "SingleEnemy", "AllEnemies"
        public List<CardEffectConfig> Effects { get; set; } = new List<CardEffectConfig>();
    }
}
