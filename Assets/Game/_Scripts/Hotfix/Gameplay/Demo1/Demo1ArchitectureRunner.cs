using UnityEngine;

namespace Game.Gameplay.Demo1
{
    public class Demo1ArchitectureRunner : MonoBehaviour
    {
        private void Awake()
        {
            Demo1Architecture.Launch();
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

