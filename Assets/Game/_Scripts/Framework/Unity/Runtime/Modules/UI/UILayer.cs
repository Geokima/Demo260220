namespace Framework.Modules.UI
{
    /// <summary>
    /// UI 层级枚举
    /// </summary>
    public enum UILayer
    {
        /// <summary>
        /// 场景层
        /// </summary>
        Scene = 0,

        /// <summary>
        /// HUD 层
        /// </summary>
        HUD = 1000,

        /// <summary>
        /// 窗口层
        /// </summary>
        Window = 2000,

        /// <summary>
        /// 导航层
        /// </summary>
        Navigation = 3000,

        /// <summary>
        /// 弹窗层
        /// </summary>
        Popup = 4000,

        /// <summary>
        /// 顶层（全屏遮罩、Loading 等）
        /// </summary>
        Top = 5000,
    }
}
