using System;
using Game.Base;
using Newtonsoft.Json;

namespace Game.DTOs
{
    #region Login

    [Serializable]
    public class LoginRequest : BaseRequest
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("userId")]
        public int UserId { get; set; }
    }

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

    public class LoginResponse : BaseResponse<LoginData> { }

    #endregion

    #region Register

    [Serializable]
    public class RegisterRequest : BaseRequest
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }
    }

    [Serializable]
    public class RegisterData
    {
        [JsonProperty("userId")]
        public int UserId { get; set; }
    }

    public class RegisterResponse : BaseResponse<RegisterData> { }

    #endregion

    #region Logout

    [Serializable]
    public class LogoutRequest : BaseRequest
    {
    }

    [Serializable]
    public class LogoutData { }

    public class LogoutResponse : BaseResponse<LogoutData> { }

    #endregion
}
