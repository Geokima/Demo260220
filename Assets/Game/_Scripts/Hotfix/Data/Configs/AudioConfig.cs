using Framework.Modules.Config;

namespace Game.Config
{
    public class AudioConfig : IConfigRow
    {
        public int Id { get; set; }
        public string AssetFullPath { get; set; } = string.Empty;
        public float Volume { get; set; }
        public int Priority { get; set; }
    }
}
