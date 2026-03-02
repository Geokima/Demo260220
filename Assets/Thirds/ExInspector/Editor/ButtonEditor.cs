using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Framework.Utils;

namespace Framework.Editor
{
    [CustomEditor(typeof(MonoBehaviour), true)]
    [CanEditMultipleObjects]
    public class ButtonEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var methods = target.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.GetCustomAttribute<ButtonAttribute>() != null)
                .ToList();

            if (methods.Count == 0) return;

            string currentGroup = null;
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<ButtonAttribute>();
                var group = attr.Group ?? "";
                var label = string.IsNullOrEmpty(attr.Label) ? method.Name : attr.Label;

                if (group != currentGroup)
                {
                    if (!string.IsNullOrEmpty(group))
                    {
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField(group, EditorStyles.boldLabel);
                    }
                    currentGroup = group;
                }

                if (GUILayout.Button(label))
                {
                    foreach (var t in targets)
                        method.Invoke(t, null);
                }
            }
        }
    }
}
