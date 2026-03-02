namespace Framework.Modules.Scene
{
    /// <summary>
    /// 场景加载开始事件
    /// </summary>
    public struct SceneLoadStartEvent
    {
        /// <summary>
        /// 场景名称
        /// </summary>
        public string SceneName;
    }

    /// <summary>
    /// 场景加载进度事件
    /// </summary>
    public struct SceneLoadProgressEvent
    {
        /// <summary>
        /// 正在加载的场景列表
        /// </summary>
        public string[] SceneNames;

        /// <summary>
        /// 当前总进度 (0-1)
        /// </summary>
        public float Progress;
    }

    /// <summary>
    /// 场景加载完成事件
    /// </summary>
    public struct SceneLoadCompleteEvent
    {
        /// <summary>
        /// 加载完成的场景列表
        /// </summary>
        public string[] SceneNames;
    }

    /// <summary>
    /// 场景加载错误事件
    /// </summary>
    public struct SceneErrorEvent
    {
        /// <summary>
        /// 出错的场景名称
        /// </summary>
        public string SceneName;

        /// <summary>
        /// 错误详情描述
        /// </summary>
        public string Error;
    }
}
