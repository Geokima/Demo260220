using Framework.Modules.Http;
using Framework.Modules.Network;
using Framework.Modules.Procedure;
using Framework.Modules.Scene;
using Framework.Unity.Bridge;
using Game.Models;
using Game.Procedures;
using Game.Services;
using UnityEngine;

namespace Game
{
    public class GameManager : MonoBehaviour
    {
        [Header("Server")]
        public string ProductionServerUrl = "";
        public string TestServerUrl = "http://localhost:8080";
        public string TestWebSocketUrl = "ws://localhost:8081";
        public bool IsTestServer = true;

        public static GameManager Instance { get; private set; }
        public static Transform Transform => Instance?.transform;
        public static GameObject GameObject => Instance?.gameObject;

        private void Awake()
        {
            Instance ??= this;
            gameObject.AddComponent<ArchitectureDriver>();
            GameArchitecture.OnRegisterPatch += architecture =>
            {
                // Services
                architecture.RegisterSystem(new AuthService());
                architecture.RegisterSystem(new PlayerService());
                architecture.RegisterSystem(new ResourceService());
                architecture.RegisterSystem(new InventoryService());

                // Models
                architecture.RegisterModel(new AccountModel());
                architecture.RegisterModel(new PlayerModel());
                architecture.RegisterModel(new InventoryModel());
            };

            LaunchGame();
        }

        private void LaunchGame()
        {
            GameArchitecture.Launch();

            // HttpSystem 配置
            var httpSystem = GameArchitecture.Instance.GetSystem<IHttpSystem>();
            httpSystem.SetConfig(ProductionServerUrl, TestServerUrl, IsTestServer);

            // Procedures 注册
            var procedureSystem = GameArchitecture.Instance.GetSystem<IProcedureSystem>();
            procedureSystem.RegisterProcedure(new LaunchProcedure());
            procedureSystem.RegisterProcedure(new PreloadProcedure());
            procedureSystem.RegisterProcedure(new LoginProcedure());
            procedureSystem.RegisterProcedure(new MainProcedure());

            // 启动流程
            var procedure = GameArchitecture.Instance.GetSystem<IProcedureSystem>();
            procedure.Start<LaunchProcedure>();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
            {
                GameArchitecture.Instance.GetSystem<INetworkSystem>()?.OnApplicationResume();
            }
        }
    }
}
