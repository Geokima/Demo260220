using Framework;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Game.Models;
using Framework.Modules.Http;

namespace Game.Services
{
    /// <summary>
    /// 认证服务 - 处理登录/注册等网络请求（无状态）
    /// </summary>
    public class AuthService : AbstractSystem
    {
        private bool _isProcessing;
        public bool IsProcessing => _isProcessing;

        /// <summary>
        /// 登录请求
        /// </summary>
        public async UniTaskVoid LoginAsync(string username, string password)
        {
            if (_isProcessing)
            {
                Debug.LogWarning("[Auth] Already processing");
                return;
            }

            _isProcessing = true;
            Debug.Log($"[Auth] Logging in: {username}");

            try
            {
                var httpSystem = this.GetSystem<HttpSystem>();
                var json = JsonUtility.ToJson(new LoginRequest { username = username, password = password });
                var result = await httpSystem.PostAsync("/login", json);

                if (string.IsNullOrEmpty(result))
                {
                    this.SendEvent(new LoginFailedEvent { Error = "网络错误" });
                    return;
                }

                var response = JsonUtility.FromJson<LoginResponse>(result);

                if (response.code == 0)
                {
                    Debug.Log($"<color=green>✓ 登录成功</color> UserId:{response.userId}");
                    
                    // 如果已有账号登录，先清除旧数据
                    var accountModel = this.GetModel<AccountModel>();
                    if (accountModel.IsLoggedIn)
                    {
                        Debug.Log("[Auth] 检测到已登录账号，清除旧数据");
                        Logout();
                    }
                    
                    // 写入新账号数据
                    accountModel.Token.Value = response.token;
                    accountModel.UserId.Value = response.userId;
                    accountModel.Username.Value = response.username;

                    // 发送登录成功事件（PlayerService会监听并同步数据）
                    this.SendEvent(new LoginSuccessEvent { Token = response.token, UserId = response.userId });
                }
                else
                {
                    Debug.LogWarning($"<color=red>✗ 登录失败:</color> {response.msg}");
                    this.SendEvent(new LoginFailedEvent { Error = response.msg });
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Auth] Login error: {ex.Message}");
                this.SendEvent(new LoginFailedEvent { Error = "网络错误" });
            }
            finally
            {
                _isProcessing = false;
            }
        }

        /// <summary>
        /// 注册请求
        /// </summary>
        public async UniTaskVoid RegisterAsync(string username, string password)
        {
            if (_isProcessing)
            {
                Debug.LogWarning("[Auth] Already processing");
                return;
            }

            _isProcessing = true;
            Debug.Log($"[Auth] Registering: {username}");

            try
            {
                var httpSystem = this.GetSystem<HttpSystem>();
                var json = JsonUtility.ToJson(new RegisterRequest { username = username, password = password });
                var result = await httpSystem.PostAsync("/register", json);

                if (string.IsNullOrEmpty(result))
                {
                    this.SendEvent(new RegisterFailedEvent { Error = "网络错误" });
                    return;
                }

                var response = JsonUtility.FromJson<RegisterResponse>(result);

                if (response.code == 0)
                {
                    Debug.Log($"<color=green>✓ 注册成功</color> UserId:{response.userId}");
                    this.SendEvent(new RegisterSuccessEvent { UserId = response.userId, Username = username });
                }
                else
                {
                    Debug.LogWarning($"<color=red>✗ 注册失败:</color> {response.msg}");
                    this.SendEvent(new RegisterFailedEvent { Error = response.msg });
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Auth] Register error: {ex.Message}");
                this.SendEvent(new RegisterFailedEvent { Error = "网络错误" });
            }
            finally
            {
                _isProcessing = false;
            }
        }

        /// <summary>
        /// 退出登录
        /// </summary>
        public void Logout()
        {
            // 发送退出登录事件
            this.SendEvent(new LogoutEvent());
            
            // 清除所有模型数据
            this.GetModel<AccountModel>().Clear();
            this.GetModel<PlayerModel>().Clear();
            this.GetModel<InventoryModel>().Clear();
            
            Debug.Log("[Auth] Logout - 所有数据已清除");
        }

        [System.Serializable]
        private class LoginRequest
        {
            public string username;
            public string password;
        }

        private class LoginResponse
        {
            public int code;
            public string msg;
            public string token;
            public int userId;
            public string username;
        }

        [System.Serializable]
        private class RegisterRequest
        {
            public string username;
            public string password;
        }

        private class RegisterResponse
        {
            public int code;
            public string msg;
            public int userId;
        }
    }
}
