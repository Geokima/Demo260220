using Framework;
using Cysharp.Threading.Tasks;

namespace Game.Auth
{
    /// <summary> 登录命令 </summary>
    public class LoginCommand : AbstractCommand
    {
        public string Username;
        public string Password;

        public override void Execute(object sender)
        {
            this.GetSystem<AuthService>().LoginAsync(Username, Password).Forget();
        }
    }

    /// <summary> 退出登录命令 </summary>
    public class LogoutCommand : AbstractCommand
    {
        public override void Execute(object sender)
        {
            this.GetSystem<AuthService>().Logout().Forget();
        }
    }

    /// <summary> 注册账号命令 </summary>
    public class RegisterCommand : AbstractCommand
    {
        /// <summary> 账号 </summary>
        public string Username;
        /// <summary> 密码 </summary>
        public string Password;

        public override void Execute(object sender)
        {
            this.GetSystem<AuthService>().RegisterAsync(Username, Password).Forget();
        }
    }
}
