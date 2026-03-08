using Framework;
using Game.Auth;
using Game.DTOs;
using Game.Base;
using UnityEngine;

namespace Game.Auth
{
    /// <summary>
    /// 认证代理 - 负责同步登录/注册后的账户数据
    /// </summary>
    public class AuthSyncer : BaseSyncer
    {
        public override void Init()
        {
            // 认证模块主要是 Request-Response 模式，
            // 但如果未来有 WebSocket 推送的 Token 过期，可以在这里监听。
        }

        /// <summary>
        /// 同步登录回包数据到 AccountModel
        /// </summary>
        public void SyncLoginResponse(LoginResponse response)
        {
            if (response == null || response.Code != 0 || response.Data == null) return;

            var accountModel = this.GetModel<AccountModel>();

            accountModel.Token.Value = response.Data.Token;
            accountModel.UserId.Value = response.Data.UserId;
            accountModel.Username.Value = response.Data.Username;

            ServerGateway.WsUrl = response.Data.WsUrl;

            Debug.Log($"[AuthSyncer] 账户数据已对齐: {response.Data.Username}");
        }

        /// <summary>
        /// 清除账户数据（登出时调用）
        /// </summary>
        public void ClearAccount()
        {
            this.GetModel<AccountModel>().Clear();
            Debug.Log("[AuthSyncer] 账户数据已清除");
        }
    }
}
