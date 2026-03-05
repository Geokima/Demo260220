// MailDTOs.cs

using System;
using System.Collections.Generic;

namespace MailSystem
{
    public class MailData
    {
        public string MailId { get; set; }
        public string Sender { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }
        public List<MailAttachment> Attachments { get; set; }
        public bool ReadStatus { get; set; }
        public DateTime ExpiryTime { get; set; }
        public DateTime CreatedTime { get; set; }
    }

    public class MailAttachment
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
    }

    public class MailResponse
    {
        public string MailId { get; set; }
        public bool IsSuccessful { get; set; }
        public string ResponseMessage { get; set; }
    }
}