using System.Collections.Generic;
using Framework.Modules.Config;

namespace Game.Gameplay.Demo1
{
    public class Demo1EnemyConfig : IConfigRow
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int MaxHP { get; set; }
        public List<int> CardIds { get; set; } = new List<int>();
    }
}
