using Framework.Modules.Config;
using System.Collections.Generic;
using Game.DTOs;

namespace Game.Config
{
    public class MissionConfig : IConfigRow
    {
        public int Id { get; set; }
        public string MissionId => Id.ToString();
        public string Type { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        
        public string ConditionType { get; set; }
        public int TargetProgress { get; set; }
        public string ConditionParam { get; set; } 

        public string PreMissionId { get; set; }

        public string StartTime { get; set; }
        public string EndTime { get; set; }

        public List<ObtainItem> Rewards { get; set; } 
        public int SortOrder { get; set; }
        public bool Available { get; set; }
    }
}