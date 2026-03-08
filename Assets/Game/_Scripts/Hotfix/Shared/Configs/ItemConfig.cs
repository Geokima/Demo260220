using Framework.Modules.Config;
using System; // Added for [Serializable]
using System.Collections.Generic; // Added for List
using Newtonsoft.Json; // Added for [JsonProperty]

namespace Game.Config
{
    public class ItemConfig : IConfigRow
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public int MaxStack { get; set; }
        public int EffectId { get; set; }
    }
}
