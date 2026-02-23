namespace Framework.Modules.Scene
{
    public struct SceneLoadStartEvent
    {
        public string SceneName;
    }

    public struct SceneLoadProgressEvent
    {
        public string[] SceneNames;
        public float Progress;
    }

    public struct SceneLoadCompleteEvent
    {
        public string[] SceneNames;
    }

    public struct SceneErrorEvent
    {
        public string SceneName;
        public string Error;
    }
}