namespace Game.Consts
{
    /// <summary>
    /// 业务消息类型常量定义 (WebSocket JSON 协议中的 type 字段)
    /// </summary>
    public static class NetworkMsgType
    {
        public const string BindToken = "bind_token";
        public const string ForceLogout = "force_logout";
        public const string Announcement = "announcement";
        public const string Chat = "chat";
        public const string PlayerSync = "player_sync";
        public const string Echo = "echo";
        public const string InventoryUpdate = "inventory_update";
        public const string MailUpdate = "mail_update";
        public const string MissionUpdate = "mission_update";
    }
}
