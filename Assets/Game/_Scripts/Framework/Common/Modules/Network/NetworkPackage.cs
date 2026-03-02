namespace Framework.Modules.Network
{
    /// <summary>
    /// 网络消息包结构
    /// </summary>
    public struct NetworkPackage
    {
        /// <summary>
        /// 指令号
        /// </summary>
        public int Cmd;

        /// <summary>
        /// 原始数据负载
        /// </summary>
        public byte[] Payload;

        /// <summary>
        /// JSON 数据字符串（如果适用）
        /// </summary>
        public string JsonData;

        public NetworkPackage(int cmd, byte[] payload)
        {
            Cmd = cmd;
            Payload = payload;
            JsonData = null;
        }

        public NetworkPackage(int cmd, string jsonData)
        {
            Cmd = cmd;
            JsonData = jsonData;
            Payload = System.Text.Encoding.UTF8.GetBytes(jsonData);
        }
    }
}
