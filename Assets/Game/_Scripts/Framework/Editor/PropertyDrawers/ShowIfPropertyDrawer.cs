#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

using Framework.Utils;

namespace Framework.Editor
{
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (ShouldShow(property))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return ShouldShow(property) ? EditorGUI.GetPropertyHeight(property, label, true) : 0;
        }

        private bool ShouldShow(SerializedProperty property)
        {
            var showIf = attribute as ShowIfAttribute;
            if (showIf == null) return true;

            var target = property.serializedObject.targetObject;
            var conditionField = GetField(target, showIf.ConditionField);

            if (conditionField != null)
            {
                var value = conditionField.GetValue(target);
                return Equals(value, showIf.ExpectedValue);
            }

            var conditionProperty = property.serializedObject.FindProperty(showIf.ConditionField);
            if (conditionProperty != null)
            {
                return GetPropertyValue(conditionProperty) == showIf.ExpectedValue;
            }

            return true;
        }

        private FieldInfo GetField(object target, string fieldName)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null) return field;
                type = type.BaseType;
            }
            return null;
        }

        private bool GetPropertyValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    return property.boolValue;
                case SerializedPropertyType.Integer:
                    return property.intValue != 0;
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue != null;
                case SerializedPropertyType.Enum:
                    return property.enumValueIndex != 0;
                default:
                    return true;
            }
        }
    }
}
#endif
