using Framework;

namespace Game.Mail
{
    public class GetMailListCommand : AbstractCommand
    {
        public override void Execute(object sender)
        {
            this.GetSystem<MailService>().RequestGetMailList();
        }
    }

    public class ReadMailCommand : AbstractCommand
    {
        public string MailId;

        public override void Execute(object sender)
        {
            this.GetSystem<MailService>().RequestReadMail(MailId);
        }
    }

    public class ReceiveAttachmentCommand : AbstractCommand
    {
        public string MailId;

        public override void Execute(object sender)
        {
            this.GetSystem<MailService>().RequestReceiveAttachment(MailId);
        }
    }

    public class DeleteMailCommand : AbstractCommand
    {
        public string MailId;

        public override void Execute(object sender)
        {
            this.GetSystem<MailService>().RequestDeleteMail(MailId);
        }
    }
}
