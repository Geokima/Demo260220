using System.Collections.Generic;
using Framework;
using Game.DTOs;

namespace Game.Mail
{
    public class MailModel : AbstractModel
    {
        public BindableDictionary<string, MailData> Mails = new();
        // 增量维护的未读数，UI 直接访问 .Value
        public BindableProperty<int> UnreadCount = new(0);
        public BindableProperty<long> Revision = new(0);

        // 补回：TestController 需要的查询接口
        public List<MailData> GetAllMails()
        {
            return new List<MailData>(Mails.Values);
        }

        public MailData GetMail(string mailId) => Mails.TryGetValue(mailId, out var mail) ? mail : null;

        // 核心同步逻辑：对标背包系统，处理增量包
        public bool SyncDiff(MailSyncData data)
        {
            if (data == null || (data.Revision > 0 && data.Revision <= Revision.Value)) 
                return false;

            // 处理删除
            if (data.RemovedIds != null)
            {
                foreach (var id in data.RemovedIds)
                {
                    if (Mails.TryGetValue(id, out var mail))
                    {
                        if (!mail.IsRead) UnreadCount.Value--;
                        Mails.Remove(id);
                    }
                }
            }

            // 处理变动
            if (data.ChangedMails != null)
            {
                foreach (var mail in data.ChangedMails)
                {
                    if (Mails.TryGetValue(mail.MailId, out var oldMail))
                    {
                        if (oldMail.IsRead != mail.IsRead)
                            UnreadCount.Value += mail.IsRead ? -1 : 1;
                        Mails[mail.MailId] = mail;
                    }
                    else
                    {
                        Mails[mail.MailId] = mail;
                        if (!mail.IsRead) UnreadCount.Value++;
                    }
                }
            }

            Revision.Value = data.Revision;
            return true;
        }

        public void SyncAll(MailListData data)
        {
            Mails.Clear();
            int unread = 0;
            if (data?.Mails != null)
            {
                foreach (var mail in data.Mails)
                {
                    Mails[mail.MailId] = mail;
                    if (!mail.IsRead) unread++;
                }
            }
            UnreadCount.Value = unread;
            Revision.Value = data?.Revision ?? 0;
        }

        public override void Deinit() => Clear();
        public void Clear() { Mails.Clear(); UnreadCount.Value = 0; Revision.Value = 0; }
    }
}