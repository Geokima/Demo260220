using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Framework.Modules.Procedure;
using UnityEngine;

namespace Game.Procedures
{
    /// <summary>
    /// 启动流程 - 检查权限和初始化
    /// </summary>
    public class LaunchProcedure : ProcedureBase
    {

#if UNITY_ANDROID
        private string[] _permissions = new string[]
        {
            // "android.permission.WRITE_EXTERNAL_STORAGE",
            // "android.permission.READ_EXTERNAL_STORAGE",
            // "android.permission.INTERNET"
        };
#endif

        public override void OnEnter()
        {
            LaunchAsync().Forget();
        }

        private async UniTaskVoid LaunchAsync()
        {
            Debug.Log("[LaunchProcedure] 启动流程开始");

#if UNITY_ANDROID && !UNITY_EDITOR
            // 检查并请求安卓权限
            await CheckPermissionsAsync();
#endif
            // 等待一帧确保初始化完成
            await UniTask.Yield();

            Debug.Log("[LaunchProcedure] 启动流程完成，进入预加载");
            ChangeProcedure<PreloadProcedure>();
        }

#if UNITY_ANDROID
        private async UniTask CheckPermissionsAsync()
        {
            var permissionsToRequest = new List<string>();

            foreach (var permission in _permissions)
            {
                if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission))
                {
                    permissionsToRequest.Add(permission);
                    Debug.Log($"[LaunchProcedure] 请求权限: {permission}");
                }
            }

            if (permissionsToRequest.Count > 0)
            {
                // 请求权限
                foreach (var permission in permissionsToRequest)
                {
                    UnityEngine.Android.Permission.RequestUserPermission(permission);
                }

                // 等待用户响应（简单延迟，实际应该监听回调）
                await UniTask.Delay(500);

                // 检查权限结果
                foreach (var permission in permissionsToRequest)
                {
                    var granted = UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission);
                    Debug.Log($"[LaunchProcedure] 权限 {permission}: {(granted ? "已授权" : "未授权")}");
                }
            }
            else
            {
                Debug.Log("[LaunchProcedure] 所有权限已授权");
            }
        }
#endif

    }
}
