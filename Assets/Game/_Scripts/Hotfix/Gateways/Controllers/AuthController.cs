using System;
using Game.Base;
using Game.Consts;
using Game.DTOs;
using UnityEngine;

namespace Game.Gateways
{
    public static class AuthController
    {
        public static LoginResponse HandleLogin(ServerContext ctx, LoginRequest req)
        {
            if (req == null)
                return new LoginResponse { Code = (int)ErrorCode.InvalidParams, Msg = "请求无效" };

            int userId = 0;
            string username = null;

            if (req.UserId > 0)
            {
                userId = req.UserId;
                username = ctx.Db.GetPlayer(req.UserId)?.Username;
            }
            else if (!string.IsNullOrEmpty(req.Username))
            {
                username = req.Username;
                userId = ctx.Db.GetUserIdByUsername(username);
            }

            if (userId <= 0 || !ctx.Db.HasUser(userId))
                return new LoginResponse { Code = (int)ErrorCode.UserNotFound, Msg = "用户不存在" };

            var token = $"token_{userId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

            Debug.Log($"[AuthController] 用户登录: UserId={userId}, Username={username}");

            return new LoginResponse
            {
                Code = 0,
                Msg = "Success",
                Data = new LoginData
                {
                    Token = token,
                    UserId = userId,
                    Username = username,
                    WsUrl = ""
                }
            };
        }

        public static RegisterResponse HandleRegister(ServerContext ctx, RegisterRequest req)
        {
            var username = req?.Username ?? $"Player{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            var newUserId = ctx.Db.GetNextUserId(username);
            Debug.Log($"[AuthController] 用户注册: UserId={newUserId}, Username={username}");
            return new RegisterResponse
            {
                Code = 0,
                Msg = "Success",
                Data = new RegisterData { UserId = newUserId }
            };
        }

        public static LogoutResponse HandleLogout(ServerContext ctx, LogoutRequest req)
        {
            // 在真后端处可能会清理 Token Session，Mock 环境暂时直显成功
            return new LogoutResponse { Code = 0, Msg = "Success" };
        }
    }
}
