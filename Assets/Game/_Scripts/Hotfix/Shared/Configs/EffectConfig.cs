using Framework.Modules.Config;

namespace Game.Config
{
    public class EffectConfig : IConfigRow
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string Vfx { get; set; }
        public string Sfx { get; set; }
        public string Params { get; set; }
    }
}
