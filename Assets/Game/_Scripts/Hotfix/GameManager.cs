using Framework.Modules.Http;
using Framework.Modules.Procedure;
using Game.Models;
using Game.Services;
using Game.Procedures;
using UnityEngine;
using Framework;

namespace Game
{
    public class GameManager : MonoBehaviour
    {
        [Header("Server")]
        public string ProductionServerUrl = "";
        public string TestServerUrl = "http://localhost:8080";
        public bool IsTestServer = true;

        public static GameManager Instance { get; private set; }
        public static Transform Transform => Instance?.transform;
        public static GameObject GameObject => Instance?.gameObject;

        private void Awake()
        {
            Instance ??= this;
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
            var httpSystem = GameArchitecture.Instance.GetSystem<HttpSystem>();
            httpSystem.SetConfig(ProductionServerUrl, TestServerUrl, IsTestServer);

            // Procedures 注册
            var procedureSystem = GameArchitecture.Instance.GetSystem<ProcedureSystem>();
            procedureSystem.RegisterProcedure(new LaunchProcedure());
            procedureSystem.RegisterProcedure(new PreloadProcedure());
            procedureSystem.RegisterProcedure(new LoginProcedure());
            procedureSystem.RegisterProcedure(new MainProcedure());

            // 启动流程
            var procedure = GameArchitecture.Instance.GetSystem<ProcedureSystem>();
            procedure.Start<LaunchProcedure>();
        }

        private void Update()
        {
            GameArchitecture.Instance.GetSystem<ProcedureSystem>()?.Update();
        }

        private void FixedUpdate()
        {
            GameArchitecture.Instance.GetSystem<ProcedureSystem>()?.FixedUpdate();
        }
    }
}
