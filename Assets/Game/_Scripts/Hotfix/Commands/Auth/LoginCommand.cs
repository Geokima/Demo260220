using Framework;
using Game.Services;

namespace Game.Commands
{
    public class LoginCommand : AbstractCommand
    {
        public string Username;
        public string Password;

        public override void Execute(object sender)
        {
            this.GetSystem<AuthService>().LoginAsync(Username, Password).Forget();
        }
    }
}
