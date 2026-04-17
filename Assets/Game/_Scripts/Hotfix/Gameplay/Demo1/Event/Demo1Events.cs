namespace Game.Gameplay.Demo1.Event
{
    // 选择遭遇事件
    public struct SelectSceneModeEvent
    {
        public SceneMode Mode { get; set; }

        public SelectSceneModeEvent(SceneMode mode)
        {
            Mode = mode;
        }
    }

    // 退出遭遇事件
    public struct QuitSceneEvent { }
    public struct CollectRewardEvent { }
}
