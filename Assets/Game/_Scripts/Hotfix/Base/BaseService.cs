using Framework;
using Game.Gateways;

namespace Game.Base
{
    /// <summary>
    /// 所有 Service 的基类 - 负责业务流程编排
    /// </summary>
    public abstract class BaseService : AbstractSystem
    {
        protected IServerGateway NetworkClient => this.GetSystem<IServerGateway>();


        /// <summary>
        /// 获取指定类型的 Syncer
        /// </summary>
        protected T GetSyncer<T>() where T : class, ISystem => this.GetSystem<T>();
    }
}
