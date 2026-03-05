namespace Framework.Modules.Http
{
    /// <summary>
    /// HTTP 状态事件，用于通知加载状态变化
    /// </summary>
    public struct HttpStatusUpdateEvent : IEvent
    {
        /// <summary>
        /// 请求的 URL
        /// </summary>
        public string Url;

        /// <summary>
        /// 是否正在加载中
        /// </summary>
        public bool IsLoading;
    }

    /// <summary>
    /// HTTP 错误事件，用于通知请求失败
    /// </summary>
    public struct HttpErrorEvent : IEvent
    {
        /// <summary>
        /// 请求的 URL
        /// </summary>
        public string Url;

        /// <summary>
        /// HTTP 状态码
        /// </summary>
        public long StatusCode;

        /// <summary>
        /// 错误详情描述
        /// </summary>
        public string Error;
    }
}
