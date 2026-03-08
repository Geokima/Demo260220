using Cysharp.Threading.Tasks;
using Framework;
using Game.Auth;
using Game.Base;
using Game.DTOs;

namespace Game.Mission
{
    public class MissionService : BaseService
    {
        public override void Init()
        {
            this.RegisterEvent<LoginSuccessEvent>(OnLoginSuccess);
        }

        private async void OnLoginSuccess(LoginSuccessEvent e)
        {
            var response = await ServerGateway.PostAsync<MissionListResponse>("/mission/list");
            if (response?.Code == 0)
                this.GetSyncer<MissionSyncer>().SyncMissionListResponse(response);
        }

        public async UniTask ReportMissionProgressAsync(string conditionType)
        {
            var response = await ServerGateway.PostAsync<MissionProgressRequest, MissionProgressResponse>(
                "/mission/progress", new MissionProgressRequest { ConditionType = conditionType });

            if (response?.Code == 0)
                this.GetSyncer<MissionSyncer>().SyncMissionProgressResponse(response);
        }

        public async UniTask ClaimMissionAsync(string missionId)
        {
            var response = await ServerGateway.PostAsync<ClaimMissionRequest, ClaimMissionResponse>(
                "/mission/claim", new ClaimMissionRequest { MissionId = missionId });

            if (response?.Code == 0 && response.Data != null)
            {
                this.GetSyncer<MissionSyncer>().SyncMissionListResponse(new MissionListResponse { 
                    Data = new MissionListData { Missions = response.Data.Missions, Revision = response.Data.Revision }
                });
                this.SendEvent(new MissionClaimedEvent { MissionId = missionId });
            }
        }
    }
}