using Framework;

namespace Game
{
    /// <summary>
    /// 登录成功事件
    /// </summary>
    public struct LoginSuccessEvent : IEvent
    {
        /// <summary>登录令牌</summary>
        public string Token;
        /// <summary>用户ID</summary>
        public int UserId;
    }

    /// <summary>
    /// 登录失败事件
    /// </summary>
    public struct LoginFailedEvent : IEvent
    {
        /// <summary>错误信息</summary>
        public string Error;
    }

    /// <summary>
    /// 注册成功事件
    /// </summary>
    public struct RegisterSuccessEvent : IEvent
    {
        /// <summary>用户ID</summary>
        public int UserId;
        /// <summary>用户名</summary>
        public string Username;
    }

    /// <summary>
    /// 注册失败事件
    /// </summary>
    public struct RegisterFailedEvent : IEvent
    {
        /// <summary>错误信息</summary>
        public string Error;
    }
}
