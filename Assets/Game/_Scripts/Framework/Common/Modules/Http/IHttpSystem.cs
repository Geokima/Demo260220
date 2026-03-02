using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Framework;

namespace Framework.Modules.Http
{
    /// <summary>
    /// HTTP 系统接口
    /// </summary>
    public interface IHttpSystem : ISystem
    {
        /// <summary>
        /// 正式环境地址
        /// </summary>
        string ProdUrl { get; set; }

        /// <summary>
        /// 测试环境地址
        /// </summary>
        string TestUrl { get; set; }

        /// <summary>
        /// 是否为测试环境
        /// </summary>
        bool IsTest { get; set; }

        /// <summary>
        /// 当前使用的基础地址
        /// </summary>
        string BaseUrl { get; }

        /// <summary>
        /// 设置配置
        /// </summary>
        /// <param name="prodUrl">正式地址</param>
        /// <param name="testUrl">测试地址</param>
        /// <param name="isTest">是否测试环境</param>
        void SetConfig(string prodUrl, string testUrl, bool isTest);

        /// <summary>
        /// 添加全局请求头
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        void AddHeader(string key, string value);

        /// <summary>
        /// 发起 GET 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="callBack">回调函数</param>
        void Get(string url, Action<string> callBack = null);

        /// <summary>
        /// 发起 POST 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="json">JSON 数据</param>
        /// <param name="callBack">回调函数</param>
        void Post(string url, string json = null, Action<string> callBack = null);

        /// <summary>
        /// 异步发起 GET 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="token">取消令牌</param>
        /// <returns>响应内容</returns>
        UniTask<string> GetAsync(string url, CancellationToken token = default);

        /// <summary>
        /// 异步发起 POST 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="json">JSON 数据</param>
        /// <param name="token">取消令牌</param>
        /// <returns>响应内容</returns>
        UniTask<string> PostAsync(string url, string json = null, CancellationToken token = default);
    }
}
