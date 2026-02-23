using Framework.Modules.Http;
using Framework.Modules.Procedure;
using Game.Systems;
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
        public static Transform Transform => Instance.transform;
        public static GameObject GameObject => Instance.gameObject;

        private void Awake()
        {
            Instance = this;
            GameArchitecture.OnRegisterPatch += architecture =>
            {
                // Services
                architecture.RegisterSystem(new LoginService());
                architecture.RegisterSystem(new ResourceService());
                architecture.RegisterSystem(new InventoryService());

                // Systems
                architecture.RegisterSystem(new LoginSystem());

                // Models
                architecture.RegisterModel(new PlayerModel());
                architecture.RegisterModel(new InventoryModel());
            };
            GameArchitecture.Launch();
            var httpSystem = GameArchitecture.Instance.GetSystem<HttpSystem>();
            httpSystem.SetConfig(ProductionServerUrl, TestServerUrl, IsTestServer);

            // Procedures
            var procedureSystem = GameArchitecture.Instance.GetSystem<ProcedureSystem>();
            procedureSystem.RegisterProcedure(new PreloadProcedure());
            procedureSystem.RegisterProcedure(new LoginProcedure());
            procedureSystem.RegisterProcedure(new MainProcedure());
            // 启动流程
            var procedure = GameArchitecture.Instance.GetSystem<ProcedureSystem>();
            procedure.Start(ProcedureType.Preload);

            Debug.Log("GameManager initialized");
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
