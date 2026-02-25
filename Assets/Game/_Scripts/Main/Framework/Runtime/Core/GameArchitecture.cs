using Framework;
using Framework.Modules.Config;
using Framework.Modules.Http;
using Framework.Modules.Pool;
using Framework.Modules.Procedure;
using Framework.Modules.Res;
using Framework.Modules.Scene;
using Framework.Modules.UI;
using UnityEngine;

namespace Game
{
    public class GameArchitecture : Architecture<GameArchitecture>
    {
        protected override void RegisterModule()
        {
            RegisterSystem(new ConfigSystem());
            RegisterSystem(new HttpSystem());
            RegisterSystem(new ResSystem());
            RegisterSystem(new PoolSystem());
            RegisterSystem(new ProcedureSystem());
            RegisterSystem(new SceneSystem());
            RegisterSystem(new UISystem());
        }
    }
}