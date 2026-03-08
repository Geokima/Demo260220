using Framework.Modules.Config;

namespace Game.Config
{
    public class SceneConfig : IConfigRow
    {
        public int Id { get; set; }
        public string SceneGroup { get; set; } = string.Empty;
        public string AssetFullPath { get; set; } = string.Empty;
        public string BGMId { get; set; } = string.Empty;
    }
}
