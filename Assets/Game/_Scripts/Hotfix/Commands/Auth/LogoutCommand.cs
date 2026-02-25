using Framework;
using Game.Services;

namespace Game.Commands
{
    /// <summary>
    /// 退出登录命令
    /// </summary>
    public class LogoutCommand : AbstractCommand
    {
        public override void Execute(object sender)
        {
            this.GetSystem<AuthService>().Logout();
        }
    }
}
