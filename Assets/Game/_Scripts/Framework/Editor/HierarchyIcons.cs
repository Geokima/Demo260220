#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Framework.Editor
{
    [InitializeOnLoad]
    public static class HierarchyIcons
    {
        private static Texture2D _warningIcon;
        private static Texture2D _errorIcon;
        private static Texture2D _uiIcon;

        static HierarchyIcons()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
            LoadIcons();
        }

        private static void LoadIcons()
        {
            _warningIcon = EditorGUIUtility.IconContent("console.warnicon").image as Texture2D;
            _errorIcon = EditorGUIUtility.IconContent("console.erroricon").image as Texture2D;
            _uiIcon = EditorGUIUtility.IconContent("Canvas Icon").image as Texture2D;
        }

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (go == null) return;

            float x = selectionRect.xMax - 20;
            float y = selectionRect.y + 2;
            var iconRect = new Rect(x, y, 16, 16);

            // 检查缺失引用
            if (HasMissingReference(go))
            {
                GUI.DrawTexture(iconRect, _errorIcon);
                return;
            }

            // 检查 UI 物体
            if (go.GetComponent<RectTransform>() != null)
            {
                iconRect.x -= 18;
                GUI.DrawTexture(iconRect, _uiIcon);
            }

            // 检查脚本错误
            if (HasScriptError(go))
            {
                GUI.DrawTexture(iconRect, _warningIcon);
            }
        }

        private static bool HasMissingReference(GameObject go)
        {
            var components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null) return true;

                var so = new SerializedObject(comp);
                var prop = so.GetIterator();
                while (prop.NextVisible(true))
                {
                    if (prop.propertyType == SerializedPropertyType.ObjectReference &&
                        prop.objectReferenceValue == null &&
                        prop.objectReferenceInstanceIDValue != 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool HasScriptError(GameObject go)
        {
            var components = go.GetComponents<Component>();
            foreach (var comp in components)
                if (comp == null) return true;
            return false;
        }
    }
}
#endif
