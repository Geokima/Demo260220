using System;
using Newtonsoft.Json;

namespace Game.DTOs
{
    /// <summary>
    /// 登录请求
    /// </summary>
    [Serializable]
    public class LoginRequest
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }
    }

    /// <summary>
    /// 登录响应数据
    /// </summary>
    [Serializable]
    public class LoginData
    {
        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("userId")]
        public int UserId { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("wsUrl")]
        public string WsUrl { get; set; }
    }

    /// <summary>
    /// 登录响应
    /// </summary>
    public class LoginResponse : BaseResponse<LoginData> { }

    /// <summary>
    /// 注册请求
    /// </summary>
    [Serializable]
    public class RegisterRequest
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }
    }

    /// <summary>
    /// 注册响应数据
    /// </summary>
    [Serializable]
    public class RegisterData
    {
        [JsonProperty("userId")]
        public int UserId { get; set; }
    }

    /// <summary>
    /// 注册响应
    /// </summary>
    public class RegisterResponse : BaseResponse<RegisterData> { }

    /// <summary>
    /// 登出请求
    /// </summary>
    [Serializable]
    public class LogoutRequest
    {
        [JsonProperty("token")]
        public string Token { get; set; }
    }

    /// <summary>
    /// 登出响应数据
    /// </summary>
    [Serializable]
    public class LogoutData
    {
    }

    /// <summary>
    /// 登出响应
    /// </summary>
    public class LogoutResponse : BaseResponse<LogoutData> { }
}
