using Cysharp.Threading.Tasks;
using Framework;
using Framework.Modules.Network;
using Game.Auth;
using Game.Base;
using Game.Consts;
using Game.DTOs;
using UnityEngine;

namespace Game.Player
{
    public class PlayerService : BaseService
    {
        #region Fields

        #endregion

        #region Lifecycle

        public override void Init()
        {
            this.RegisterEvent<LoginSuccessEvent>(OnLoginSuccess);
            this.RegisterEvent<LogoutEvent>(OnLogout);
            this.RegisterEvent<NetworkStatusUpdateEvent>(OnNetworkStatusUpdate);
        }

        public override void Deinit()
        {
            // QFramework 的 RegisterEvent 返回的 IUnregister 会在 Deinit/Shutdown 时自动由架构处理，
            // 除非你想手动提早解绑。这里你可以保留或移除。
        }

        private void OnLogout(LogoutEvent e)
        {
            this.GetModel<PlayerModel>().Clear();
        }

        private void OnNetworkStatusUpdate(NetworkStatusUpdateEvent e)
        {
            if (e.NewStatus == NetworkStatus.Connected)
            {
                SendBindTokenAsync().Forget();
                OnReconnectAsync().Forget();
            }
        }

        private void OnLoginSuccess(LoginSuccessEvent e)
        {
            HandleLoginFlowAsync().Forget();
        }

        #endregion

        #region Public Methods

        public async UniTask<PlayerResponse> GetResourcesAsync()
        {
            return await GetResourcesInternalAsync();
        }

        #endregion

        #region Private Methods

        private async UniTaskVoid HandleLoginFlowAsync()
        {
            Debug.Log("[PlayerService] Starting Login Flow...");

            bool wsConnected = await NetworkClient.ConnectWsAsync();
            if (wsConnected)
            {
                await SendBindTokenAsync();
            }

            var resourceResponse = await GetResourcesInternalAsync();
            if (resourceResponse != null && resourceResponse.Code == 0)
            {
                this.GetSyncer<PlayerSyncer>().SyncResources(resourceResponse);
            }

            Debug.Log("[PlayerService] Login Flow Completed.");
        }

        private async UniTask SendBindTokenAsync()
        {
            var accountModel = this.GetModel<AccountModel>();
            if (!accountModel.IsLoggedIn) return;

            Debug.Log("[PlayerService] Sending WS BindToken...");

            NetworkClient.SendWsMessage(NetworkMsgType.BindToken, new { token = accountModel.Token.Value });
            await UniTask.Yield();
        }

        private async UniTaskVoid OnReconnectAsync()
        {
            var response = await GetResourcesInternalAsync();
            if (response != null && response.Code == 0)
            {
                this.GetSyncer<PlayerSyncer>().SyncResources(response);
            }
        }

        private async UniTask<PlayerResponse> GetResourcesInternalAsync()
        {
            var response = await NetworkClient.PostAsync<PlayerResponse>("/resource/get");
            return response;
        }

        #endregion
    }
}
