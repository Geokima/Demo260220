using Cysharp.Threading.Tasks;
using Framework;
using Framework.Modules.Http;
using Game.Data;
using Game.Models;
using UnityEngine;

namespace Game.Services
{
    /// <summary>
    /// 玩家服务 - 管理玩家数据同步
    /// </summary>
    public class PlayerService : AbstractSystem
    {
        private IHttpSystem _httpSystem;
        private long _lastSyncTime = 0;
        private const long SyncInterval = 300; // 5分钟同步一次（秒）

        public override void Init()
        {
            _httpSystem = this.GetSystem<IHttpSystem>();
            
            // 监听登录成功事件，自动同步数据
            this.RegisterEvent<LoginSuccessEvent>(OnLoginSuccess);
            
            // 启动定时同步
            StartPeriodicSync().Forget();
        }

        /// <summary>
        /// 登录成功后同步玩家数据
        /// </summary>
        private void OnLoginSuccess(LoginSuccessEvent e)
        {
            SyncPlayerDataAsync().Forget();
        }

        /// <summary>
        /// 消耗前同步 - 确保体力值最新
        /// </summary>
        public async UniTask SyncBeforeSpendAsync()
        {
            await SyncPlayerDataAsync();
        }

        /// <summary>
        /// 启动定时同步
        /// </summary>
        private async UniTaskVoid StartPeriodicSync()
        {
            while (true)
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(60)); // 每分钟检查一次
                
                var accountModel = this.GetModel<AccountModel>();
                if (!accountModel.IsLoggedIn) continue;
                
                long currentTime = GetCurrentTimestamp();
                long timeSinceLastSync = currentTime - _lastSyncTime;
                
                // 距离上次同步超过5分钟
                if (timeSinceLastSync >= SyncInterval)
                {
                    Debug.Log("[PlayerService] 距离上次同步5分钟，自动同步数据");
                    await SyncPlayerDataAsync();
                }
            }
        }

        private long GetCurrentTimestamp()
        {
            return System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        /// <summary>
        /// 从服务器同步玩家数据
        /// </summary>
        public async UniTask SyncPlayerDataAsync()
        {
            var accountModel = this.GetModel<AccountModel>();
            if (!accountModel.IsLoggedIn)
            {
                Debug.LogWarning("[PlayerService] 未登录，无法同步数据");
                return;
            }

            try
            {
                var token = accountModel.Token.Value;
                var json = $"{{\"token\":\"{token}\"}}";
                
                // 同步资源
                await SyncResourcesAsync(json);
                
                // 同步背包
                await SyncInventoryAsync(json);
                
                // 记录同步时间
                _lastSyncTime = GetCurrentTimestamp();
                
                Debug.Log("[PlayerService] 玩家数据同步完成");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PlayerService] 同步数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 同步资源数据
        /// </summary>
        private async UniTask SyncResourcesAsync(string json)
        {
            var result = await _httpSystem.PostAsync("/resource/get", json);
            if (string.IsNullOrEmpty(result)) return;

            var response = JsonUtility.FromJson<ResourceResponse>(result);
            if (response.code != 0) return;

            var playerModel = this.GetModel<PlayerModel>();
            playerModel.Diamond.Value = response.diamond;
            playerModel.Gold.Value = response.gold;
            playerModel.Exp.Value = response.exp;
            playerModel.Energy.Value = response.energy;
            // 使用服务器时间，避免客户端作弊
            playerModel.LastEnergyRecoverTime = response.lastEnergyTime;
            
            Debug.Log($"[PlayerService] 资源同步: 钻石={response.diamond}, 金币={response.gold}, 经验={response.exp}, 等级={response.level}, 体力={response.energy}");
        }

        /// <summary>
        /// 同步背包数据
        /// </summary>
        private async UniTask SyncInventoryAsync(string json)
        {
            var result = await _httpSystem.PostAsync("/inventory/get", json);
            if (string.IsNullOrEmpty(result)) return;

            var response = JsonUtility.FromJson<InventoryResponse>(result);
            if (response.code != 0 || response.inventory == null) return;

            var inventoryModel = this.GetModel<InventoryModel>();
            inventoryModel.SetInventory(response.inventory);
            
            Debug.Log($"[PlayerService] 背包同步: {response.inventory.items?.Length ?? 0} 个物品");
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
        private class InventoryResponse
        {
            public int code;
            public string msg;
            public InventoryData inventory;
        }
    }
}
