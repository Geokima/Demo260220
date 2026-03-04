using System.Linq;
using Framework;
using Framework.Modules.Config;
using Framework.Modules.Scene;
using Game.Configs;
using UnityEngine;

namespace Game.Scene
{
    /// <summary>
    /// 切换场景命令
    /// </summary>
    public class ChangeSceneCommand : AbstractCommand
    {
        /// <summary>场景组名称 (对应 SceneConfig.SceneGroup)</summary>
        public string SceneGroup;

        public override void Execute(object sender)
        {
            var configSystem = this.GetSystem<IConfigSystem>();
            var sceneSystem = this.GetSystem<ISceneSystem>();
            
            // 获取对应场景组的所有配置
            var configs = configSystem.GetSheet<SceneConfig>().All()
                .Where(c => c.SceneGroup == SceneGroup)
                .ToList();
            
            if (configs.Count == 0)
            {
                Debug.LogWarning($"[ChangeSceneCommand] No scenes found for group: {SceneGroup}");
                return;
            }
            
            // 获取所有场景路径
            var scenePaths = configs.Select(c => c.AssetFullPath).ToArray();
            
            Debug.Log($"[ChangeSceneCommand] Switching to scene group: {SceneGroup} ({scenePaths.Length} scenes)");
            
            // 加载场景
            if (scenePaths.Length == 1)
                sceneSystem.LoadScene(scenePaths[0]);
            else
                sceneSystem.LoadScenes(scenePaths);
        }
    }
}
