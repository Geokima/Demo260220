using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.DTOs
{
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
        [JsonProperty("attachments")] public List<MailAttachment> Attachments;
    }

    [Serializable]
    public class MailSyncData
    {
        [JsonProperty("changedMails")] public List<MailData> ChangedMails;
        [JsonProperty("removedIds")] public List<string> RemovedIds;
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
    [Serializable] public class MailOpRequest { [JsonProperty("mailId")] public string MailId; }

    [Serializable]
    public class MailAttachment
    {
        [JsonProperty("itemId")] public int ItemId;
        [JsonProperty("count")] public int Count;
    }
}