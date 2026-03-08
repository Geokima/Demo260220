using System;
using System.Collections.Generic;
using Game.Base;
using Newtonsoft.Json;

namespace Game.DTOs
{
    // 玩家个人邮件数据
    [Serializable]
    public class MailData
    {
        [JsonProperty("mailId")] public string MailId;
        [JsonProperty("title")] public string Title;
        [JsonProperty("content")] public string Content;
        [JsonProperty("sender")] public string Sender;
        [JsonProperty("createTime")] public long CreateTime;
        [JsonProperty("isRead")] public bool IsRead;
        [JsonProperty("isReceived")] public bool IsReceived;
        [JsonProperty("attachments")] public List<ObtainItem> Attachments; 
    }

    // 全服邮件原始数据（数据库存储用）
    [Serializable]
    public class BroadcastMailData
    {
        [JsonProperty("mailId")] public string MailId;
        [JsonProperty("title")] public string Title;
        [JsonProperty("content")] public string Content;
        [JsonProperty("sender")] public string Sender;
        [JsonProperty("createTime")] public long CreateTime;
        [JsonProperty("attachments")] public List<ObtainItem> Attachments;
    }

    [Serializable]
    public class MailSyncData 
    {
        [JsonProperty("changedMails")] public List<MailData> ChangedMails;
        [JsonProperty("removedIds")] public List<string> RemovedIds;
        [JsonProperty("obtainedItems")] public List<ObtainItem> ObtainedItems; 
        [JsonProperty("revision")] public long Revision;
    }

    [Serializable]
    public class MailListData
    {
        [JsonProperty("mails")] public List<MailData> Mails;
        [JsonProperty("revision")] public long Revision;
    }

    public class MailListResponse : BaseResponse<MailListData> { }
    public class MailSyncResponse : BaseResponse<MailSyncData> { }

    [Serializable] 
    public class MailOpRequest : BaseRequest 
    { 
        [JsonProperty("mailId")] public string MailId; 
    }
}