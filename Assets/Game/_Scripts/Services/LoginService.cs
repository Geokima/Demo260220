using System;
using Cysharp.Threading.Tasks;
using Framework;
using Framework.Modules.Http;
using Framework.Utils;
using Game.Models;
using Game.Systems;
using UnityEngine;

namespace Game.Services
{
    public class LoginService : AbstractSystem
    {
        private HttpSystem _httpSystem;

        public override void Init()
        {
            _httpSystem = this.GetSystem<HttpSystem>();
        }

        public void RequestLogin(string username, string password)
        {
            var hashedPassword = CryptoUtil.SHA256Hash(password);
            LoginAsync(username, hashedPassword).Forget();
        }

        private async UniTaskVoid LoginAsync(string username, string password)
        {
            try
            {
                var json = $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}";
                var response = await _httpSystem.PostAsync("/login", json);

                if (string.IsNullOrEmpty(response))
                {
                    this.SendEvent(new LoginFailedEvent { Error = "Network request failed" });
                    return;
                }

                var loginResponse = JsonUtility.FromJson<LoginResponse>(response);

                if (loginResponse.code == 0)
                {
                    var loginSystem = this.GetSystem<LoginSystem>();
                    loginSystem.SetLoginInfo(loginResponse.token, loginResponse.userId);

                    var resources = await this.GetSystem<ResourceService>().GetResourcesAsync();
                    if (resources.success)
                    {
                        var resourcesModel = this.GetModel<PlayerModel>();
                        resourcesModel.Diamond.Value = resources.diamond;
                        resourcesModel.Gold.Value = resources.gold;
                    }

                    this.SendEvent(new LoginSuccessEvent { Token = loginResponse.token, UserId = loginResponse.userId });
                }
                else
                {
                    this.SendEvent(new LoginFailedEvent { Error = loginResponse.msg });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LoginService] Login failed: {ex.Message}");
                this.SendEvent(new LoginFailedEvent { Error = $"Network error: {ex.Message}" });
            }
        }

        public async UniTask<(bool success, long diamond, long gold)> GetResourcesAsync()
        {
            if (_httpSystem == null)
            {
                Debug.LogError("[ResourceService] HttpSystem is null");
                return (false, 0, 0);
            }

            var response = await _httpSystem.PostAsync("/resource/get", "{}");

            if (string.IsNullOrEmpty(response))
            {
                Debug.LogError("[ResourceService] Get resources failed");
                return (false, 0, 0);
            }

            var result = JsonUtility.FromJson<ResourceResponse>(response);
            return (result.code == 0, result.diamond, result.gold);
        }

        [Serializable]
        private class LoginResponse
        {
            public int code;
            public string msg;
            public string token;
            public int userId;
        }

        [Serializable]
        private class ResourceResponse
        {
            public int code;
            public string msg;
            public long diamond;
            public long gold;
        }
    }

    public struct LoginSuccessEvent
    {
        public string Token;
        public int UserId;
    }

    public struct LoginFailedEvent
    {
        public string Error;
    }
}
