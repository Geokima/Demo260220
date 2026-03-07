using Framework;

namespace Game.Auth
{
    /// <summary>
    /// 账户数据模型 - 管理登录状态
    /// </summary>
    public class AccountModel : AbstractModel
    {
        /// <summary>登录令牌</summary>
        public BindableProperty<string> Token { get; } = new BindableProperty<string>("");
        
        /// <summary>用户ID</summary>
        public BindableProperty<int> UserId { get; } = new BindableProperty<int>(0);

        /// <summary>用户名</summary>
        public BindableProperty<string> Username { get; } = new BindableProperty<string>("");

        /// <summary>是否已登录</summary>
        public bool IsLoggedIn => !string.IsNullOrEmpty(Token.Value);

        /// <summary>
        /// 清除所有数据（退出登录时调用）
        /// </summary>
        public void Clear()
        {
            Token.Value = "";
            UserId.Value = 0;
            Username.Value = "";
        }
    }
}
