using Cysharp.Threading.Tasks;
using Framework;
using Game.Auth;
using Game.Base;
using Game.DTOs;

namespace Game.Mail
{
    public class MailService : BaseService
    {
        public override void Init() => this.RegisterEvent<LoginSuccessEvent>(e => RequestGetMailList().Forget());

        // 获取列表（登录后自动调用）
        public async UniTask RequestGetMailList()
        {
            var response = await ServerGateway.PostAsync<MailListResponse>("/mail/list");
            if (response?.Code == 0) this.GetSyncer<MailSyncer>().SyncAllResponse(response);
        }

        public void RequestReadMail(string id) => DoOp(id, "/mail/read").Forget();
        public void RequestReceiveAttachment(string id) => DoOp(id, "/mail/receive").Forget();
        public void RequestDeleteMail(string id) => DoOp(id, "/mail/delete").Forget();

        // 统一操作处理
        private async UniTask DoOp(string id, string url)
        {
            var response = await ServerGateway.PostAsync<MailOpRequest, MailSyncResponse>(url, new MailOpRequest { MailId = id });
            
            if (response?.Code == 0) 
            {
                // 这里的 response.Data 包含了 ChangedMails 和 Rewards
                this.GetSyncer<MailSyncer>().SyncIncremental(response.Data);
            }
            else 
            {
                this.SendEvent(new MailOperationFailedEvent { Reason = response?.Msg, MailId = id });
            }
        }
    }
}