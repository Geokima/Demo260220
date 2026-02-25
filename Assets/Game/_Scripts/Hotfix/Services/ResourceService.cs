using Cysharp.Threading.Tasks;
using Framework;
using Framework.Modules.Http;
using Game.Models;
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

        public void RequestChangeDiamond(int amount, string reason)
        {
            ChangeDiamondAsync(amount, reason).Forget();
        }

        private async UniTaskVoid ChangeDiamondAsync(int amount, string reason)
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

        private async UniTask<(bool success, int currentAmount)> RequestChangeDiamondAsync(int amount, string reason)
        {
            if (_httpSystem == null)
            {
                Debug.LogError("[ResourceService] HttpSystem is null");
                return (false, 0);
            }

            var accountModel = this.GetModel<AccountModel>();
            if (string.IsNullOrEmpty(accountModel.Token.Value))
            {
                Debug.LogError("[ResourceService] Not logged in");
                return (false, 0);
            }

            var json = $"{{\"token\":\"{accountModel.Token.Value}\",\"amount\":{amount},\"reason\":\"{reason}\"}}";
            var response = await _httpSystem.PostAsync("/resource/diamond", json);

            if (string.IsNullOrEmpty(response))
            {
                Debug.LogError("[ResourceService] Request failed");
                return (false, 0);
            }

            var result = JsonUtility.FromJson<DiamondResponse>(response);
            return (result.code == 0, result.currentAmount);
        }

        public void RequestChangeGold(int amount, string reason)
        {
            ChangeGoldAsync(amount, reason).Forget();
        }

        private async UniTaskVoid ChangeGoldAsync(int amount, string reason)
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

        private async UniTask<(bool success, int currentAmount)> RequestChangeGoldAsync(int amount, string reason)
        {
            if (_httpSystem == null)
            {
                Debug.LogError("[ResourceService] HttpSystem is null");
                return (false, 0);
            }

            var accountModel = this.GetModel<AccountModel>();
            if (string.IsNullOrEmpty(accountModel.Token.Value))
            {
                Debug.LogError("[ResourceService] Not logged in");
                return (false, 0);
            }

            var json = $"{{\"token\":\"{accountModel.Token.Value}\",\"amount\":{amount},\"reason\":\"{reason}\"}}";
            var response = await _httpSystem.PostAsync("/resource/gold", json);

            if (string.IsNullOrEmpty(response))
            {
                Debug.LogError("[ResourceService] Request failed");
                return (false, 0);
            }

            var result = JsonUtility.FromJson<GoldResponse>(response);
            return (result.code == 0, result.currentAmount);
        }

        public async UniTask<(bool success, int diamond, int gold)> GetResourcesAsync()
        {
            if (_httpSystem == null)
            {
                Debug.LogError("[ResourceService] HttpSystem is null");
                return (false, 0, 0);
            }

            var accountModel = this.GetModel<AccountModel>();
            if (string.IsNullOrEmpty(accountModel.Token.Value))
            {
                Debug.LogError("[ResourceService] Not logged in");
                return (false, 0, 0);
            }

            var json = $"{{\"token\":\"{accountModel.Token.Value}\"}}";
            var response = await _httpSystem.PostAsync("/resource/get", json);

            if (string.IsNullOrEmpty(response))
            {
                Debug.LogError("[ResourceService] Get resources failed");
                return (false, 0, 0);
            }

            var result = JsonUtility.FromJson<ResourceResponse>(response);
            return (result.code == 0, result.diamond, result.gold);
        }

        public void RequestChangeExp(int amount, string reason)
        {
            ChangeExpAsync(amount, reason).Forget();
        }

        private async UniTaskVoid ChangeExpAsync(int amount, string reason)
        {
            var playerModel = this.GetModel<PlayerModel>();

            if (amount < 0 && playerModel.Exp.Value < -amount)
            {
                this.SendEvent(new ExpChangeFailedEvent { Reason = "经验不足" });
                return;
            }

            var (success, currentExp, currentLevel) = await RequestChangeExpAsync(amount, reason);

            if (success)
            {
                playerModel.Exp.Value = currentExp;
                // 等级由服务器计算返回，本地更新
                int oldLevel = playerModel.Level.Value;
                if (currentLevel != oldLevel)
                {
                    // 使用反射或直接修改内部值（这里简化处理）
                    this.SendEvent(new LevelUpEvent { OldLevel = oldLevel, NewLevel = currentLevel });
                }
                this.SendEvent(new ExpChangedEvent { Amount = amount, Current = currentExp, Level = currentLevel });
            }
            else
            {
                this.SendEvent(new ExpChangeFailedEvent { Reason = "服务器拒绝" });
            }
        }

        private async UniTask<(bool success, int currentExp, int currentLevel)> RequestChangeExpAsync(int amount, string reason)
        {
            if (_httpSystem == null)
            {
                Debug.LogError("[ResourceService] HttpSystem is null");
                return (false, 0, 1);
            }

            var accountModel = this.GetModel<AccountModel>();
            if (string.IsNullOrEmpty(accountModel.Token.Value))
            {
                Debug.LogError("[ResourceService] Not logged in");
                return (false, 0, 1);
            }

            var json = $"{{\"token\":\"{accountModel.Token.Value}\",\"amount\":{amount},\"reason\":\"{reason}\"}}";
            var response = await _httpSystem.PostAsync("/resource/exp", json);

            if (string.IsNullOrEmpty(response))
            {
                Debug.LogError("[ResourceService] Request exp failed");
                return (false, 0, 1);
            }

            var result = JsonUtility.FromJson<ExpResponse>(response);
            return (result.code == 0, result.currentExp, result.currentLevel);
        }

        /// <summary>
        /// 请求变更体力
        /// 注意：是否可超出上限由服务器根据reason判断
        /// </summary>
        public void RequestChangeEnergy(int amount, string reason)
        {
            ChangeEnergyAsync(amount, reason).Forget();
        }

        private async UniTaskVoid ChangeEnergyAsync(int amount, string reason)
        {
            var playerModel = this.GetModel<PlayerModel>();

            // 消耗前同步，确保体力值最新
            if (amount < 0)
            {
                await this.GetSystem<PlayerService>().SyncBeforeSpendAsync();
            }

            if (amount < 0 && playerModel.Energy.Value < -amount)
            {
                this.SendEvent(new EnergyChangeFailedEvent { Reason = "体力不足" });
                return;
            }

            var (success, currentEnergy) = await RequestChangeEnergyAsync(amount, reason);

            if (success)
            {
                playerModel.Energy.Value = currentEnergy;
                var maxEnergy = playerModel.GetMaxEnergy();
                this.SendEvent(new EnergyChangedEvent { Amount = amount, Current = currentEnergy, Max = maxEnergy });
            }
            else
            {
                this.SendEvent(new EnergyChangeFailedEvent { Reason = "服务器拒绝" });
            }
        }

        private async UniTask<(bool success, int currentEnergy)> RequestChangeEnergyAsync(int amount, string reason)
        {
            if (_httpSystem == null)
            {
                Debug.LogError("[ResourceService] HttpSystem is null");
                return (false, 0);
            }

            var accountModel = this.GetModel<AccountModel>();
            if (string.IsNullOrEmpty(accountModel.Token.Value))
            {
                Debug.LogError("[ResourceService] Not logged in");
                return (false, 0);
            }

            var json = $"{{\"token\":\"{accountModel.Token.Value}\",\"amount\":{amount},\"reason\":\"{reason}\"}}";
            var response = await _httpSystem.PostAsync("/resource/energy", json);

            if (string.IsNullOrEmpty(response))
            {
                Debug.LogError("[ResourceService] Request energy failed");
                return (false, 0);
            }

            var result = JsonUtility.FromJson<EnergyResponse>(response);
            return (result.code == 0, result.currentEnergy);
        }

        [System.Serializable]
        private class DiamondResponse
        {
            public int code;
            public string msg;
            public int currentAmount;
        }

        [System.Serializable]
        private class GoldResponse
        {
            public int code;
            public string msg;
            public int currentAmount;
        }

        [System.Serializable]
        private class ResourceResponse
        {
            public int code;
            public string msg;
            public int diamond;
            public int gold;
            public int exp;
            public int level;
            public int energy;
            public long lastEnergyTime;
        }

        [System.Serializable]
        private class ExpResponse
        {
            public int code;
            public string msg;
            public int currentExp;
            public int currentLevel;
        }

        [System.Serializable]
        private class EnergyResponse
        {
            public int code;
            public string msg;
            public int currentEnergy;
        }

        [System.Serializable]
        private class ExchangeResponse
        {
            public int code;
            public string msg;
            public int currentDiamond;
            public int currentGold;
        }

        /// <summary>
        /// 请求钻石兑换金币
        /// </summary>
        public void RequestExchangeDiamondForGold(int diamondAmount, int goldAmount)
        {
            ExchangeDiamondForGoldAsync(diamondAmount, goldAmount).Forget();
        }

        private async UniTaskVoid ExchangeDiamondForGoldAsync(int diamondAmount, int goldAmount)
        {
            var playerModel = this.GetModel<PlayerModel>();

            // 客户端预检查
            if (!playerModel.HasEnough(PlayerModel.ResourceType.Diamond, diamondAmount))
            {
                this.SendEvent(new DiamondChangeFailedEvent { Reason = "钻石不足" });
                return;
            }

            var (success, currentDiamond, currentGold) = await RequestExchangeDiamondForGoldAsync(diamondAmount, goldAmount);

            if (success)
            {
                playerModel.Diamond.Value = currentDiamond;
                playerModel.Gold.Value = currentGold;
                this.SendEvent(new DiamondChangedEvent { Amount = -diamondAmount, Current = currentDiamond });
                this.SendEvent(new GoldChangedEvent { Amount = goldAmount, Current = currentGold });
            }
            else
            {
                this.SendEvent(new DiamondChangeFailedEvent { Reason = "兑换失败" });
            }
        }

        private async UniTask<(bool success, int currentDiamond, int currentGold)> RequestExchangeDiamondForGoldAsync(int diamondAmount, int goldAmount)
        {
            if (_httpSystem == null)
            {
                Debug.LogError("[ResourceService] HttpSystem is null");
                return (false, 0, 0);
            }

            var accountModel = this.GetModel<AccountModel>();
            if (string.IsNullOrEmpty(accountModel.Token.Value))
            {
                Debug.LogError("[ResourceService] Not logged in");
                return (false, 0, 0);
            }

            var json = $"{{\"token\":\"{accountModel.Token.Value}\",\"diamondAmount\":{diamondAmount},\"goldAmount\":{goldAmount}}}";
            var response = await _httpSystem.PostAsync("/exchange/diamond_to_gold", json);

            if (string.IsNullOrEmpty(response))
            {
                Debug.LogError("[ResourceService] Exchange request failed");
                return (false, 0, 0);
            }

            var result = JsonUtility.FromJson<ExchangeResponse>(response);
            return (result.code == 0, result.currentDiamond, result.currentGold);
        }
    }

}
