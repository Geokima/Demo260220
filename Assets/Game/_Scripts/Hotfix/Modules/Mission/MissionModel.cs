using Framework;
using Game.DTOs;

namespace Game.Mission
{
    public class MissionModel : AbstractModel
    {
        public BindableDictionary<string, BindableProperty<MissionData>> Missions = new();
        public BindableProperty<long> Revision = new BindableProperty<long>(0);

        public bool SyncDiff(MissionData[] updatedMissions, long newRevision)
        {
            if (newRevision <= Revision.Value && newRevision != 0) return false;

            if (updatedMissions != null)
            {
                foreach (var mission in updatedMissions)
                {
                    if (Missions.TryGetValue(mission.MissionId, out var missionProp))
                        missionProp.Value = mission;
                    else
                        Missions[mission.MissionId] = new BindableProperty<MissionData>(mission);
                }
            }

            Revision.Value = newRevision;
            return true;
        }

        public void SyncAll(MissionData[] missions, long newRevision)
        {
            Missions.Clear();
            SyncDiff(missions, newRevision);
        }

        public void Clear()
        {
            Missions.Clear();
            Revision.Value = 0;
        }
    }
}