using Cysharp.Threading.Tasks;
using Framework;
using Framework.Modules.Http;
using Game.Models;
using Game.Systems;
using UnityEngine;

namespace Game.Services
{
    public class ResourceService : AbstractSystem
    {
        private HttpSystem _httpSystem;

        public override void Init()
        {
            _httpSystem = this.GetSystem<HttpSystem>();
        }

        public void RequestChangeDiamond(long amount, string reason)
        {
            ChangeDiamondAsync(amount, reason).Forget();
        }

        private async UniTaskVoid ChangeDiamondAsync(long amount, string reason)
        {
            var resourcesModel = this.GetModel<PlayerModel>();

            if (amount < 0 && !resourcesModel.HasEnough(PlayerModel.ResourceType.Diamond, -amount))
            {
                this.SendEvent(new DiamondChangeFailedEvent { Reason = "钻石不足" });
                return;
            }

            var (success, currentAmount) = await RequestChangeDiamondAsync(amount, reason);

            if (success)
            {
                resourcesModel.Diamond.Value = currentAmount;
                this.SendEvent(new DiamondChangedEvent { Amount = amount, Current = currentAmount });
            }
            else
            {
                this.SendEvent(new DiamondChangeFailedEvent { Reason = "服务器拒绝" });
            }
        }

        private async UniTask<(bool success, long currentAmount)> RequestChangeDiamondAsync(long amount, string reason)
        {
            if (_httpSystem == null)
            {
                Debug.LogError("[ResourceService] HttpSystem is null");
                return (false, 0);
            }

            var loginSystem = this.GetSystem<LoginSystem>();
            if (string.IsNullOrEmpty(loginSystem.Token))
            {
                Debug.LogError("[ResourceService] Not logged in");
                return (false, 0);
            }

            var json = $"{{\"token\":\"{loginSystem.Token}\",\"amount\":{amount},\"reason\":\"{reason}\"}}";
            var response = await _httpSystem.PostAsync("/resource/diamond", json);

            if (string.IsNullOrEmpty(response))
            {
                Debug.LogError("[ResourceService] Request failed");
                return (false, 0);
            }

            var result = JsonUtility.FromJson<DiamondResponse>(response);
            return (result.code == 0, result.currentAmount);
        }

        public void RequestChangeGold(long amount, string reason)
        {
            ChangeGoldAsync(amount, reason).Forget();
        }

        private async UniTaskVoid ChangeGoldAsync(long amount, string reason)
        {
            var resourcesModel = this.GetModel<PlayerModel>();

            if (amount < 0 && !resourcesModel.HasEnough(PlayerModel.ResourceType.Gold, -amount))
            {
                this.SendEvent(new GoldChangeFailedEvent { Reason = "金币不足" });
                return;
            }

            var (success, currentAmount) = await RequestChangeGoldAsync(amount, reason);

            if (success)
            {
                resourcesModel.Gold.Value = currentAmount;
                this.SendEvent(new GoldChangedEvent { Amount = amount, Current = currentAmount });
            }
            else
            {
                this.SendEvent(new GoldChangeFailedEvent { Reason = "服务器拒绝" });
            }
        }

        private async UniTask<(bool success, long currentAmount)> RequestChangeGoldAsync(long amount, string reason)
        {
            if (_httpSystem == null)
            {
                Debug.LogError("[ResourceService] HttpSystem is null");
                return (false, 0);
            }

            var loginSystem = this.GetSystem<LoginSystem>();
            if (string.IsNullOrEmpty(loginSystem.Token))
            {
                Debug.LogError("[ResourceService] Not logged in");
                return (false, 0);
            }

            var json = $"{{\"token\":\"{loginSystem.Token}\",\"amount\":{amount},\"reason\":\"{reason}\"}}";
            var response = await _httpSystem.PostAsync("/resource/gold", json);

            if (string.IsNullOrEmpty(response))
            {
                Debug.LogError("[ResourceService] Request failed");
                return (false, 0);
            }

            var result = JsonUtility.FromJson<GoldResponse>(response);
            return (result.code == 0, result.currentAmount);
        }

        public async UniTask<(bool success, long diamond, long gold)> GetResourcesAsync()
        {
            if (_httpSystem == null)
            {
                Debug.LogError("[ResourceService] HttpSystem is null");
                return (false, 0, 0);
            }

            var loginSystem = this.GetSystem<LoginSystem>();
            if (string.IsNullOrEmpty(loginSystem.Token))
            {
                Debug.LogError("[ResourceService] Not logged in");
                return (false, 0, 0);
            }

            var json = $"{{\"token\":\"{loginSystem.Token}\"}}";
            var response = await _httpSystem.PostAsync("/resource/get", json);

            if (string.IsNullOrEmpty(response))
            {
                Debug.LogError("[ResourceService] Get resources failed");
                return (false, 0, 0);
            }

            var result = JsonUtility.FromJson<ResourceResponse>(response);
            return (result.code == 0, result.diamond, result.gold);
        }

        [System.Serializable]
        private class DiamondResponse
        {
            public int code;
            public string msg;
            public long currentAmount;
        }

        [System.Serializable]
        private class GoldResponse
        {
            public int code;
            public string msg;
            public long currentAmount;
        }

        [System.Serializable]
        private class ResourceResponse
        {
            public int code;
            public string msg;
            public long diamond;
            public long gold;
        }
    }

}
