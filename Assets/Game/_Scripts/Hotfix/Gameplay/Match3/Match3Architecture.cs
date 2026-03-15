using Framework;

namespace Game.Match3
{
    /// <summary>
    /// 三消子架构容器
    /// 确保三消逻辑与全局业务隔离，支持独立的 Launch/Shutdown 生命周期
    /// </summary>
    public class Match3Architecture : Architecture<Match3Architecture>
    {
        protected override void RegisterModule()
        {
            // 注册三消专属模型
            this.RegisterModel(new Match3Model());
            
            // 注册三消专属服务（系统）
            this.RegisterSystem<IMatch3Service>(new Match3Service());
        }
    }
}
