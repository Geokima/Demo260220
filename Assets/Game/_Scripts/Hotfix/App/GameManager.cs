using Framework.Modules.Http;
using Framework.Modules.Network;
using Framework.Modules.Procedure;
using Game.Auth;
using Game.Player;
using Game.Inventory;
using Game.Mail;
using Game.Shop;
using Game.Procedures;
using Game.Gateways;
using Game.Effect;
using Game.Mission;
using UnityEngine;

namespace Game
{
    public class GameManager : MonoBehaviour
    {
        [Header("Server")]
        public string ProductionServerUrl = "";
        public string TestServerUrl = "http://localhost:8080";
        public bool IsTestServer = true;
        public bool IsLocalServer = true;

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
                if (IsLocalServer)
                    architecture.RegisterSystem<IServerGateway>(new LocalServerGateway());
                else
                    architecture.RegisterSystem<IServerGateway>(new NetworkServerGateway());

                // Service & Syncers
                architecture.RegisterSystem(new AuthService());
                architecture.RegisterSystem(new AuthSyncer());
                architecture.RegisterSystem(new PlayerService());
                architecture.RegisterSystem(new PlayerSyncer());
                architecture.RegisterSystem(new InventoryService());
                architecture.RegisterSystem(new InventorySyncer());
                architecture.RegisterSystem(new MailService());
                architecture.RegisterSystem(new MailSyncer());
                architecture.RegisterSystem(new ShopService());
                architecture.RegisterSystem(new ShopSyncer());
                architecture.RegisterSystem(new MissionService());
                architecture.RegisterSystem(new MissionSyncer());

                // System
                architecture.RegisterSystem(new EffectSystem());

                // Models
                architecture.RegisterModel(new AccountModel());
                architecture.RegisterModel(new PlayerModel());
                architecture.RegisterModel(new InventoryModel());
                architecture.RegisterModel(new MailModel());
                architecture.RegisterModel(new MissionModel());
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
                // TODO: 考虑通过接口统一分发 Resume 事件
                GameArchitecture.Instance.GetSystem<INetworkSystem>()?.OnApplicationResume();
            }
        }

        private void OnApplicationQuit()
        {
            GameArchitecture.Instance.Shutdown();
        }   
    }
}
