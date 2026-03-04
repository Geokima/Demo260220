using Framework;
using Game.Base;
using Game.Consts;
using Game.DTOs;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Game.Player
{
    public class PlayerSyncer : BaseSyncer
    {
        public override void Init()
        {
            RegisterWsHandler(NetworkMsgType.ForceLogout, OnForceLogoutReceived);
            RegisterWsHandler(NetworkMsgType.Announcement, OnAnnouncementReceived);
            RegisterWsHandler(NetworkMsgType.PlayerSync, OnPlayerSyncReceived);
        }

        public void SyncResources(PlayerResponse response)
        {
            if (response == null || response.Code != 0 || response.Data == null) return;
            var data = response.Data;
            var playerModel = this.GetModel<PlayerModel>();
            playerModel.Gold.Value = data.Gold;
            playerModel.Diamond.Value = data.Diamond;
            playerModel.Exp.Value = data.Exp;
            playerModel.Energy.Value = data.Energy;
        }

        private void OnPlayerSyncReceived(JToken data)
        {
            var playerData = data["player"]?.ToObject<PlayerData>();
            if (playerData != null)
            {
                var playerModel = this.GetModel<PlayerModel>();
                playerModel.Gold.Value = playerData.Gold;
                playerModel.Diamond.Value = playerData.Diamond;
                playerModel.Exp.Value = playerData.Exp;
                playerModel.Energy.Value = playerData.Energy;
            }
        }

        private void OnForceLogoutReceived(JToken data)
        {
            string msg = data["msg"]?.ToString() ?? "被强制下线";
            Debug.LogWarning($"[PlayerSyncer] 收到强制下线通知: {msg}");
        }

        private void OnAnnouncementReceived(JToken data)
        {
            string message = data["message"]?.ToString() ?? "";
            if (!string.IsNullOrEmpty(message))
            {
                Debug.Log($"[PlayerSyncer] 收到公告: {message}");
            }
        }
    }
}
