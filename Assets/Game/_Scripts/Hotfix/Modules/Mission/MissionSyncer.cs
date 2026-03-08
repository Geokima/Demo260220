using Framework;
using Game.Base;
using Game.DTOs;
using Game.Consts;
using Newtonsoft.Json.Linq;

namespace Game.Mission
{
    public class MissionSyncer : BaseSyncer
    {
        public override void Init()
        {
            RegisterWsHandler(NetworkMsgType.MissionUpdate, OnMissionUpdateReceived);
        }

        private void UpdateMissionWithData(MissionData[] missions, long revision)
        {
            if (this.GetModel<MissionModel>().SyncDiff(missions, revision))
            {
                this.SendEvent(new MissionListUpdatedEvent());
            }
        }

        public void SyncMissionListResponse(MissionListResponse response)
        {
            if (response?.Data != null)
                UpdateMissionWithData(response.Data.Missions, response.Data.Revision);
        }

        public void SyncMissionProgressResponse(MissionProgressResponse response)
        {
            if (response?.Data?.UpdatedMissions != null)
                UpdateMissionWithData(response.Data.UpdatedMissions, response.Data.Revision);
        }

        private void OnMissionUpdateReceived(JToken data)
        {
            var missions = data["missions"]?.ToObject<MissionData[]>();
            var rev = data["revision"]?.Value<long>() ?? 0;
            if (missions != null) UpdateMissionWithData(missions, rev);
        }
    }
}