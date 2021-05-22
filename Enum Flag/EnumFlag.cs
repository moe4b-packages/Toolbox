using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

using System.Reflection;

namespace MB
{
    /// <summary>
    /// an attribute to be used on fields with an enum flag to allow setting multiple values for that field
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class EnumFlagAttribute : PropertyAttribute
    {
#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(EnumFlagAttribute))]
        public class Drawer : PropertyDrawer
        {
            SerializedProperty property;
            Type type;

            void Init(SerializedProperty reference)
            {
                if (property?.propertyPath == reference?.propertyPath) return;

                property = reference;

                type = MUtility.SerializedPropertyType.Retrieve(property);
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                Init(property);

                return EditorGUIUtility.singleLineHeight;
            }

            public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
            {
                Init(property);

                var value = (Enum)Enum.ToObject(type, property.longValue);

                value = EditorGUI.EnumFlagsField(rect, label, value);

                property.longValue = ChangeType<long>(value);
            }

            //Static Utility

            public static T ChangeType<T>(object value) => (T)Convert.ChangeType(value, typeof(T));
        }
#endif
    }
}