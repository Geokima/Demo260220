using Framework;
using Game.DTOs;
using System.Collections.Generic;

namespace Game.Mail
{
    public struct MailSyncEvent : IEvent 
    { 
        public MailSyncData SyncData; 
    }

    public struct MailOperationFailedEvent : IEvent 
    { 
        public string Reason; 
        public string MailId; 
    }
}