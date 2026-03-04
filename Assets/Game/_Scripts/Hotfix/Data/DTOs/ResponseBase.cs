using System;
using Newtonsoft.Json;

namespace Game.DTOs
{
    /// <summary>
    /// 基础响应结构
    /// </summary>
    [Serializable]
    public class BaseResponse<T> where T : class, new()
    {
        /// <summary>响应码，0表示成功</summary>
        [JsonProperty("code")]
        public int Code { get; set; }

        /// <summary>响应消息</summary>
        [JsonProperty("msg")]
        public string Msg { get; set; }

        /// <summary>响应数据</summary>
        [JsonProperty("data")]
        public T Data { get; set; }
    }
}
