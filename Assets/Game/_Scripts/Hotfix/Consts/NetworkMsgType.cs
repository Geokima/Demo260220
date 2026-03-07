namespace Game.Consts
{
    /// <summary>
    /// 业务消息类型常量定义 (WebSocket JSON 协议中的 type 字段)
    /// </summary>
    public static class NetworkMsgType
    {
        /// <summary>绑定 Token 请求</summary>
        public const string BindToken = "bind_token";

        /// <summary>强制下线通知</summary>
        public const string ForceLogout = "force_logout";

        /// <summary>系统公告</summary>
        public const string Announcement = "announcement";

        /// <summary>聊天消息</summary>
        public const string Chat = "chat";

        /// <summary>玩家同步</summary>
        public const string PlayerSync = "player_sync";

        /// <summary>回声测试</summary>
        public const string Echo = "echo";
        
        /// <summary>物品更新</summary>
        public const string InventoryUpdate = "inventory_update";

        /// <summary>邮件更新</summary>
        public const string MailUpdate = "mail_update";

    }
}
