using Cysharp.Threading.Tasks;
using Framework.Modules.Http;
using Game;
using UnityEngine;

namespace Game.Tests
{
    public class HttpExternalTest : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private string _externalUrl = "https://httpbin.org/get";
        [SerializeField] private string _localUrl = "/login";

        private void OnEnable()
        {
            HttpSystem.OnRequestSent += OnRequestSent;
        }

        private void OnDisable()
        {
            HttpSystem.OnRequestSent -= OnRequestSent;
        }

        private void OnRequestSent(string method, string url, string requestBody, string responseBody, long durationMs, bool isSuccess, int statusCode)
        {
            string color = durationMs > 150 ? "red" : "green";
            Debug.Log($"[HttpTest] <color={color}>[{durationMs}ms]</color> {method} {url} - Success: {isSuccess} ({statusCode})");
        }

        [ContextMenu("测试外部服务器 (httpbin.org)")]
        public void TestExternal()
        {
            var httpSystem = GameArchitecture.Instance.GetSystem<IHttpSystem>();
            if (httpSystem == null)
            {
                Debug.LogError("HttpSystem 未找到，请确保游戏已启动（GameManager 已运行）");
                return;
            }

            Debug.Log($"开始测试外部服务器: {_externalUrl}");
            httpSystem.Get(_externalUrl, result =>
            {
                Debug.Log($"外部服务器响应长度: {result?.Length ?? 0}");
            });
        }

        [ContextMenu("测试本地服务器")]
        public void TestLocal()
        {
            var httpSystem = GameArchitecture.Instance.GetSystem<IHttpSystem>();
            if (httpSystem == null)
            {
                Debug.LogError("HttpSystem 未找到");
                return;
            }

            Debug.Log($"开始测试本地服务器: {httpSystem.BaseUrl}{_localUrl}");
            // 发送一个空的登录请求，只是为了测试延迟
            httpSystem.Post(_localUrl, "{}", result =>
            {
                Debug.Log($"本地服务器响应长度: {result?.Length ?? 0}");
            });
        }
    }
}
