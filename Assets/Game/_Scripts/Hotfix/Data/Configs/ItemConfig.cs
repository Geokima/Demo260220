using Framework.Modules.Config;

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
