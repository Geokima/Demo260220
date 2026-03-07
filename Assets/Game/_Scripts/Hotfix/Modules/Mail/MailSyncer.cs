using Framework;
using Game.Base;
using Game.Consts;
using Game.DTOs;
using Newtonsoft.Json.Linq;

namespace Game.Mail
{
    public class MailSyncer : BaseSyncer
    {
        public override void Init() => RegisterWsHandler(NetworkMsgType.MailUpdate, OnMailWsPush);

        public void SyncAllResponse(MailListResponse response)
        {
            if (response?.Data == null || response.Code != 0) return;
            this.GetModel<MailModel>().SyncAll(response.Data);
            this.SendEvent(new MailSyncEvent { SyncData = null }); 
        }

        public void SyncIncremental(MailSyncData data)
        {
            if (this.GetModel<MailModel>().SyncDiff(data))
                this.SendEvent(new MailSyncEvent { SyncData = data });
        }

        private void OnMailWsPush(JToken data) => SyncIncremental(data.ToObject<MailSyncData>());
    }
}