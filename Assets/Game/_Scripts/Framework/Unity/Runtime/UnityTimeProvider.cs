using UnityEngine;
using Framework;

namespace Framework.Unity.Runtime
{
    /// <summary>
    /// Unity 环境下的时间提供者实现
    /// </summary>
    public class UnityTimeProvider : AbstractSystem, ITimeProvider
    {
        /// <inheritdoc />
        public float Time => UnityEngine.Time.time;

        /// <inheritdoc />
        public float DeltaTime => UnityEngine.Time.deltaTime;

        /// <inheritdoc />
        public float RealtimeSinceStartup => UnityEngine.Time.realtimeSinceStartup;

        public override void Init() { }

        public override void Deinit() { }
    }
}
