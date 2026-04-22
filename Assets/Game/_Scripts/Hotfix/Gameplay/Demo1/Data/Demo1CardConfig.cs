using System.Collections.Generic;
using Framework.Modules.Config;

namespace Game.Gameplay.Demo1
{
    public class Demo1CardConfig : IConfigRow
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Price { get; set; }
        public int Size { get; set; }
        public string Rank { get; set; }
        public string Type { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public int Damage { get; set; }
        public int Shield { get; set; }
        public int Poison { get; set; }
        public int Cure { get; set; }
        public int BulletCount { get; set; }
        public float MaxCD { get; set; }
    }
}
