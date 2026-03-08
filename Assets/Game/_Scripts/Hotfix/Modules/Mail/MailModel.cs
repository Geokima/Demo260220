using System.Collections.Generic;
using Framework;
using Game.DTOs;

namespace Game.Mail
{
    public class MailModel : AbstractModel
    {
        public BindableDictionary<string, MailData> Mails = new();
        public BindableProperty<int> UnreadCount = new(0);
        public BindableProperty<long> Revision = new(0);

        public List<MailData> GetAllMails() => new List<MailData>(Mails.Values);

        public bool SyncDiff(MailSyncData data)
        {
            // 严格对齐 Inv 逻辑：版本号必须大于当前版本
            if (data == null || data.Revision <= Revision.Value) return false;

            // 1. 处理删除：直接 Remove
            if (data.RemovedIds != null)
            {
                foreach (var id in data.RemovedIds)
                {
                    Mails.Remove(id);
                }
            }

            // 2. 处理变动：直接赋值覆盖（这是字典同步的最稳做法）
            if (data.ChangedMails != null)
            {
                foreach (var mail in data.ChangedMails)
                {
                    Mails[mail.MailId] = mail;
                }
            }

            // 3. 重新计算未读数（而不是加加减减，这样最不会错）
            int unread = 0;
            foreach (var m in Mails.Values)
            {
                if (!m.IsRead) unread++;
            }
            UnreadCount.Value = unread;

            // 4. 强制同步版本号
            Revision.Value = data.Revision;
            return true;
        }

        public void SyncAll(MailListData data)
        {
            Mails.Clear();
            if (data?.Mails != null)
            {
                foreach (var mail in data.Mails)
                {
                    Mails[mail.MailId] = mail;
                }
            }
            
            int unread = 0;
            foreach (var m in Mails.Values) if (!m.IsRead) unread++;
            UnreadCount.Value = unread;
            
            Revision.Value = data?.Revision ?? 0;
        }

        public override void Deinit() => Clear();
        public void Clear() { Mails.Clear(); UnreadCount.Value = 0; Revision.Value = 0; }
    }
}