using Cysharp.Threading.Tasks;
using Framework;
using Game.DTOs;
using Game.Auth;
using Game.Base;
using UnityEngine;
using Game.Player;
using Game.Inventory;

namespace Game.Auth
{
    /// <summary>
    /// 认证服务 - 处理登录/注册等网络请求流程（业务编排）
    /// </summary>
    public class AuthService : BaseService
    {
        private bool _isProcessing;
        public bool IsProcessing => _isProcessing;

        /// <summary>
        /// 登录请求流程
        /// </summary>
        public async UniTask<bool> LoginAsync(string username, string password)
        {
            if (_isProcessing)
            {
                Debug.LogWarning("[AuthService] Already processing");
                return false;
            }

            _isProcessing = true;
            Debug.Log($"[AuthService] 开始登录业务流程: {username}");

            try
            {
                // 1. 发起网络请求 (通过重构后的 NetworkClient)
                var response = await ServerGateway.PostAsync<LoginRequest, LoginResponse>("/login",
                    new LoginRequest { Username = username, Password = password });

                if (response == null)
                {
                    this.SendEvent(new LoginFailedEvent { Error = "网络连接失败" });
                    return false;
                }

                if (response.Code == 0)
                {
                    Debug.Log($"<color=green>✓ 登录成功 (Server Confirmed)</color>");

                    // 2. 调用 Syncer 同步数据 (Service 不再直接操作 Model)
                    var authSyncer = this.GetSyncer<AuthSyncer>();

                    // 如果已有账号登录，先清除旧数据
                    var accountModel = this.GetModel<AccountModel>();
                    if (accountModel.IsLoggedIn)
                    {
                        Debug.Log("[AuthService] 检测到已登录账号，清除旧数据");
                        await Logout();
                    }

                    // 让 Syncer 负责真理对齐
                    authSyncer.SyncLoginResponse(response);

                    // 3. 业务后续逻辑：发送事件（其他业务如 PlayerService 监听）
                    this.SendEvent(new LoginSuccessEvent { Token = response.Data.Token, UserId = response.Data.UserId });
                    return true;
                }
                else
                {
                    Debug.LogWarning($"<color=red>✗ 登录失败:</color> {response.Msg}");
                    this.SendEvent(new LoginFailedEvent { Error = response.Msg });
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AuthService] Login Exception: {ex.Message}");
                this.SendEvent(new LoginFailedEvent { Error = "系统错误" });
                return false;
            }
            finally
            {
                _isProcessing = false;
            }
        }

        /// <summary>
        /// 注册请求
        /// </summary>
        public async UniTask<bool> RegisterAsync(string username, string password)
        {
            if (_isProcessing)
            {
                Debug.LogWarning("[Auth] Already processing");
                return false;
            }

            _isProcessing = true;
            Debug.Log($"[Auth] Registering: {username}");

            try
            {
                var response = await ServerGateway.PostAsync<RegisterRequest, RegisterResponse>("/register",
                    new RegisterRequest { Username = username, Password = password });

                if (response == null)
                {
                    this.SendEvent(new RegisterFailedEvent { Error = "网络连接失败" });
                    return false;
                }

                if (response.Code == 0)
                {
                    Debug.Log($"<color=green>✓ 注册成功</color>");
                    this.SendEvent(new RegisterSuccessEvent { UserId = response.Data.UserId, Username = username });
                    return true;
                }
                else
                {
                    Debug.LogWarning($"<color=red>✗ 注册失败:</color> {response.Msg}");
                    this.SendEvent(new RegisterFailedEvent { Error = response.Msg });
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AuthService] Register Exception: {ex.Message}");
                this.SendEvent(new RegisterFailedEvent { Error = "系统错误" });
                return false;
            }
            finally
            {
                _isProcessing = false;
            }
        }

        /// <summary>
        /// 退出登录流程
        /// </summary>
        public async UniTask Logout()
        {
            var authSyncer = this.GetSyncer<AuthSyncer>();
            var accountModel = this.GetModel<AccountModel>();
            var token = accountModel.Token.Value;

            if (!string.IsNullOrEmpty(token))
            {
                Debug.Log("[AuthService] Sending logout request to server...");
                try
                {
                    await ServerGateway.PostAsync<LogoutRequest, object>("/logout", new LogoutRequest { Token = token });
                    ServerGateway.DisconnectWs();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[AuthService] Server logout failed (ignoring): {ex.Message}");
                }
            }

            // 发送退出登录事件
            this.SendEvent(new LogoutEvent());

            // 让 Syncer 清除数据
            authSyncer.ClearAccount();

            Debug.Log("[AuthService] Logout - 所有数据已清除");
        }
    }
}
