using System;
using UnityEngine;

namespace Framework.Modules.Timer
{
    internal class TimerDriver : MonoBehaviour
    {
        public Action OnUpdate;
        private void Update() => OnUpdate?.Invoke();
    }
}
