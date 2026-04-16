using Framework.Modules.Procedure;
using Game.Gameplay.Demo1.Procedure;
using UnityEngine;

namespace Game.Gameplay.Demo1
{
    public class Demo1ArchitectureRunner : MonoBehaviour
    {
        private void Awake()
        {
            Demo1Architecture.Launch();

            var procedureSystem = Demo1Architecture.Instance.GetSystem<IProcedureSystem>();

            procedureSystem.RegisterProcedure(new SelectionProcedure());
            procedureSystem.RegisterProcedure(new EventProcedure());
            procedureSystem.RegisterProcedure(new RewardProcedure());
            procedureSystem.RegisterProcedure(new GameOverProcedure());

            procedureSystem.Start<SelectionProcedure>();
        }

        private void Update()
        {
            Demo1Architecture.Instance.Update();
        }

        private void FixedUpdate()
        {
            Demo1Architecture.Instance.FixedUpdate();
        }
    }
}
