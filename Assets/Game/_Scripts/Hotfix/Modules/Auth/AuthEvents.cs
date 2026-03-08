using Framework;

namespace Game.Auth
{
    /// <summary> 登录成功事件 </summary>
    public struct LoginSuccessEvent : IEvent
    {
        public string Token;
        public int UserId;
    }

    /// <summary> 登录失败事件 </summary>
    public struct LoginFailedEvent : IEvent
    {
        public string Error;
    }

    /// <summary> 注册成功事件 </summary>
    public struct RegisterSuccessEvent : IEvent
    {
        public int UserId;
        public string Username;
    }

    /// <summary> 注册失败事件 </summary>
    public struct RegisterFailedEvent : IEvent
    {
        public string Error;
    }

    /// <summary> 退出登录事件 </summary>
    public struct LogoutEvent : IEvent
    {
        public string Reason;
    }
}
