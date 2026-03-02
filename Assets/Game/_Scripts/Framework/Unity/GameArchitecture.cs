using Framework;
using Framework.Modules.Config;
using Framework.Modules.Http;
using Framework.Modules.Network;
using Framework.Modules.Pool;
using Framework.Modules.Procedure;
using Framework.Modules.Res;
using Framework.Modules.Scene;
using Framework.Modules.Timer;
using Framework.Modules.UI;
using Framework.Unity.Runtime;
using UnityEngine;

namespace Game
{
    public class GameArchitecture : Architecture<GameArchitecture>
    {
        protected override void RegisterModule()
        {
            RegisterSystem<IConfigSystem>(new ConfigSystem());
            RegisterSystem<IHttpSystem>(new HttpSystem());
            RegisterSystem<INetworkSystem>(new NetworkSystem());
            RegisterSystem<IResSystem>(new ResSystem());
            RegisterSystem<IPoolSystem>(new PoolSystem());
            RegisterSystem<IProcedureSystem>(new ProcedureSystem());
            RegisterSystem<ISceneSystem>(new SceneSystem());
            RegisterSystem<ITimerSystem>(new TimerSystem());
            RegisterSystem<IUISystem>(new UISystem());
        }
    }
}