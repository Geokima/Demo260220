namespace Framework.Modules.Http
{
    using UnityEngine.Networking;

    public struct HttpStateEvent
    {
        public string Url;
        public bool IsLoading;
    }

    public struct HttpErrorEvent
    {
        public UnityWebRequest Request;
        public string Error;
    }
}