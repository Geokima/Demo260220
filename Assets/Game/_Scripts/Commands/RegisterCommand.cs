using Framework;
using Game.Services;

namespace Game.Commands
{
    /// <summary>
    /// 注册账号命令
    /// </summary>
    public class RegisterCommand : AbstractCommand
    {
        /// <summary>账号</summary>
        public string Username;
        /// <summary>密码</summary>
        public string Password;

        public override void Execute()
        {
            this.GetSystem<AuthService>().RegisterAsync(Username, Password).Forget();
        }
    }
}
