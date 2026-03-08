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
            
            // 全量更新后，抛出一个不带具体增量数据的事件，通知 UI 刷新整表
            this.SendEvent(new MailSyncEvent { SyncData = null }); 
        }

        public void SyncIncremental(MailSyncData data)
        {
            if (data == null) return;

            // 调用 Model 执行增量逻辑（内部包含 Revision 校验和 UnreadCount 重算）
            if (this.GetModel<MailModel>().SyncDiff(data))
            {
                // 只要 Model 确认数据已更新，就抛出带数据的事件
                // UI 监听此事件，通过 data.Rewards 弹窗，通过 data.ChangedMails 更新单条 UI
                this.SendEvent(new MailSyncEvent { SyncData = data });
            }
        }

        private void OnMailWsPush(JToken data) => SyncIncremental(data.ToObject<MailSyncData>());
    }
}