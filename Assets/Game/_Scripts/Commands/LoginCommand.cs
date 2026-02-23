using Framework;
using Game.Services;

namespace Game.Commands
{
    public class LoginCommand : AbstractCommand
    {
        public string Username;
        public string Password;

        public override void Execute()
        {
            this.GetSystem<LoginService>().RequestLogin(Username, Password);
        }
    }
}
