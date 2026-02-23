using System.Collections.Generic;
using Framework.Modules.Config;

namespace Game.Configs
{
    public class AudioConfig : IConfigRow
    {
        public int Id { get; set; }
        public string AssetFullPath { get; set; } = string.Empty;
        public float Volume { get; set; }
        public int Priority { get; set; }
    }

    public class SceneConfig : IConfigRow
    {
        public int Id { get; set; }
        public string SceneGroup { get; set; } = string.Empty;
        public string AssetFullPath { get; set; } = string.Empty;
        public string BGMId { get; set; } = string.Empty;
    }
}