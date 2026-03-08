using System;
using Newtonsoft.Json;

namespace Game.Base
{
    /// <summary> 空响应基类 (仅包含状态码和消息) </summary>
    [Serializable]
    public class ResponseBase
    {
        /// <summary>响应码，0表示成功</summary>
        [JsonProperty("code")]
        public int Code;

        /// <summary>响应消息</summary>
        [JsonProperty("msg")]
        public string Msg;
        
        [JsonIgnore]
        public string Message { get { return Msg; } }
    }

    /// <summary>
    /// 带数据的泛型响应结构
    /// </summary>
    [Serializable]
    public class BaseResponse<T> : ResponseBase where T : class, new()
    {
        /// <summary>响应数据</summary>
        [JsonProperty("data")]
        public T Data;
    }
}
