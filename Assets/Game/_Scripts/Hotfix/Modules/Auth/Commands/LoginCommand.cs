using Framework;
using Cysharp.Threading.Tasks;

namespace Game.Auth
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
