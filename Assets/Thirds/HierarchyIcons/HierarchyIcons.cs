using UnityEngine;
using UnityEditor;

namespace Framework.Editor
{
    [InitializeOnLoad]
    public static class HierarchyIcons
    {
        #region Fields
        private static Texture2D _warningIcon;
        private static Texture2D _errorIcon;
        private static Texture2D _canvasIcon;
        private static Texture2D _audioIcon;
        private static Texture2D _lightIcon;
        private static Texture2D _cameraIcon;
        private static Texture2D _colliderIcon;
        private static Texture2D _rigidbodyIcon;
        #endregion

        #region Lifecycle
        static HierarchyIcons()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
            LoadIcons();
        }
        #endregion

        #region GUI
        private static void LoadIcons()
        {
            _warningIcon = EditorGUIUtility.IconContent("console.warnicon").image as Texture2D;
            _errorIcon = EditorGUIUtility.IconContent("console.erroricon").image as Texture2D;
            _canvasIcon = EditorGUIUtility.IconContent("Canvas Icon").image as Texture2D;
            _audioIcon = EditorGUIUtility.IconContent("AudioSource Icon").image as Texture2D;
            _lightIcon = EditorGUIUtility.IconContent("Light Icon").image as Texture2D;
            _cameraIcon = EditorGUIUtility.IconContent("Camera Icon").image as Texture2D;
            _colliderIcon = EditorGUIUtility.IconContent("BoxCollider Icon").image as Texture2D;
            _rigidbodyIcon = EditorGUIUtility.IconContent("Rigidbody Icon").image as Texture2D;
        }

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (go == null) return;

            float x = selectionRect.xMax - 20;
            float y = selectionRect.y + 2;

            if (HasMissingReference(go))
            {
                DrawIcon(ref x, y, _errorIcon);
                return;
            }

            if (HasScriptError(go))
            {
                DrawIcon(ref x, y, _warningIcon);
            }

            if (go.GetComponent<Rigidbody>() != null || go.GetComponent<Rigidbody2D>() != null)
            {
                DrawIcon(ref x, y, _rigidbodyIcon);
            }

            if (go.GetComponent<Collider>() != null || go.GetComponent<Collider2D>() != null)
            {
                DrawIcon(ref x, y, _colliderIcon);
            }

            if (go.GetComponent<Camera>() != null)
            {
                DrawIcon(ref x, y, _cameraIcon);
            }

            if (go.GetComponent<Light>() != null || go.GetComponent("UnityEngine.Rendering.Universal.Light2D") != null)
            {
                DrawIcon(ref x, y, _lightIcon);
            }

            if (go.GetComponent<AudioSource>() != null)
            {
                DrawIcon(ref x, y, _audioIcon);
            }

            if (go.GetComponent<Canvas>() != null)
            {
                DrawIcon(ref x, y, _canvasIcon);
            }
        }

        private static void DrawIcon(ref float x, float y, Texture2D icon)
        {
            var iconRect = new Rect(x, y, 16, 16);
            GUI.DrawTexture(iconRect, icon);
            x -= 18;
        }
        #endregion

        #region Helpers
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
        #endregion
    }
}
