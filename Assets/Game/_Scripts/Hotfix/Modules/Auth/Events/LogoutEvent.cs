using Framework;

namespace Game.Auth
{
    /// <summary>
    /// 退出登录事件
    /// </summary>
    public struct LogoutEvent : IEvent
    {
        public string Reason;
    }
}
