#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace Framework.Editor
{
    public static class UINamingTool
    {
        private static readonly Dictionary<System.Type, string> _prefixMap = new Dictionary<System.Type, string>
        {
            { typeof(Button), "Btn" },
            { typeof(Text), "Txt" },
            { typeof(Image), "Img" },
            { typeof(InputField), "Input" },
            { typeof(Toggle), "Toggle" },
            { typeof(Slider), "Slider" },
            { typeof(Dropdown), "Dropdown" },
            { typeof(ScrollRect), "Scroll" },
            { typeof(Canvas), "Canvas" },
            { typeof(RectTransform), "UI" }
        };

        [MenuItem("Tools/UI Naming/Auto Rename Selected %#r")]
        private static void AutoRenameSelected()
        {
            var selected = Selection.gameObjects;
            if (selected.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "请先选择要重命名的 UI 元素", "确定");
                return;
            }

            int renamedCount = 0;
            foreach (var go in selected)
            {
                renamedCount += RenameGameObject(go);
            }

            EditorUtility.DisplayDialog("完成", $"已重命名 {renamedCount} 个物体", "确定");
        }

        [MenuItem("Tools/UI Naming/Auto Rename With Children")]
        private static void AutoRenameWithChildren()
        {
            var selected = Selection.gameObjects;
            if (selected.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "请先选择要重命名的 UI 元素", "确定");
                return;
            }

            int renamedCount = 0;
            foreach (var go in selected)
            {
                renamedCount += RenameRecursive(go);
            }

            EditorUtility.DisplayDialog("完成", $"已重命名 {renamedCount} 个物体", "确定");
        }

        private static int RenameRecursive(GameObject go)
        {
            int count = RenameGameObject(go);
            foreach (Transform child in go.transform)
            {
                count += RenameRecursive(child.gameObject);
            }
            return count;
        }

        private static int RenameGameObject(GameObject go)
        {
            string prefix = GetPrefix(go);
            if (string.IsNullOrEmpty(prefix)) return 0;

            string currentName = go.name;
            string baseName = currentName;

            // 移除已有前缀
            foreach (var p in _prefixMap.Values)
            {
                if (baseName.StartsWith(p + "_"))
                {
                    baseName = baseName.Substring(p.Length + 1);
                    break;
                }
            }

            string newName = $"{prefix}_{baseName}";
            if (newName != currentName)
            {
                Undo.RecordObject(go, "Auto Rename UI");
                go.name = newName;
                return 1;
            }
            return 0;
        }

        private static string GetPrefix(GameObject go)
        {
            var components = go.GetComponents<Component>();

            // 优先匹配特定 UI 组件
            foreach (var kvp in _prefixMap)
            {
                if (kvp.Key == typeof(RectTransform)) continue;
                if (go.GetComponent(kvp.Key) != null)
                    return kvp.Value;
            }

            // 如果是 UI 元素但没有特定组件
            if (go.GetComponent<RectTransform>() != null)
                return "UI";

            return null;
        }
    }
}
#endif
