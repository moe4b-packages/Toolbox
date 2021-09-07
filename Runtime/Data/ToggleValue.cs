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

namespace MB
{
    /// <summary>
    /// A field that provides a toggleable value
    /// </summary>
    [Serializable]
    public abstract class ToggleValue
    {
        [SerializeField]
        protected bool enabled;
        public bool Enabled { get { return enabled; } }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(ToggleValue), true)]
        public class Drawer : PropertyDrawer
        {
            SerializedProperty FindEnabledProperty(SerializedProperty property) => property.FindPropertyRelative("enabled");
            SerializedProperty FindValueProperty(SerializedProperty property) => property.FindPropertyRelative("value");

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                var enabled = FindEnabledProperty(property);
                var value = FindValueProperty(property);

                if (enabled.boolValue && value.isExpanded)
                    return EditorGUI.GetPropertyHeight(value, label, true);

                return EditorGUIUtility.singleLineHeight;
            }

            public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
            {
                var enabled = FindEnabledProperty(property);
                var value = FindValueProperty(property);

                rect = EditorGUI.IndentedRect(rect);
                EditorGUI.indentLevel = 0;

                DrawToggle(ref rect, enabled, label);

                if (enabled.boolValue)
                    DrawValue(ref rect, value, label);
                else
                    DrawLabel(ref rect, label);
            }

            protected virtual void DrawToggle(ref Rect rect, SerializedProperty enabled, GUIContent label)
            {
                var size = EditorGUIUtility.singleLineHeight - 2f;

                enabled.boolValue = EditorGUI.Toggle(new Rect(rect.x, rect.y, size, size), enabled.boolValue);

                var offset = 5f;

                rect.x += size + offset;
                rect.width -= size + offset;
            }

            protected virtual void DrawLabel(ref Rect rect, GUIContent label)
            {
                var width = GUI.skin.label.CalcSize(label).x + 10f;

                EditorGUI.LabelField(new Rect(rect.x, rect.y, width, rect.height), label);

                rect.x += width;
                rect.width -= width;
            }

            protected virtual void DrawValue(ref Rect rect, SerializedProperty value, GUIContent label)
            {
                var labelWidth = GUI.skin.label.CalcSize(label).x;

                EditorGUIUtility.labelWidth = labelWidth + 5f;

                if (HasExpandControl(value))
                {
                    var offset = 10f;

                    rect.x += offset;
                    rect.width -= offset;

                    EditorGUIUtility.labelWidth += offset;
                }

                EditorGUI.PropertyField(rect, value, label, true);
            }

            protected virtual bool HasExpandControl(SerializedProperty target)
            {
                if (target.hasVisibleChildren == false) return false;

                if (target.type.Contains("Vector")) return false;

                return true;
            }
        }
#endif
    }

    [Serializable]
    public class ToggleValue<TValue> : ToggleValue
    {
        [SerializeField]
        protected TValue value;
        public TValue Value { get { return value; } }

        public virtual TValue Evaluate() => Evaluate(default);
        public virtual TValue Evaluate(TValue fallback)
        {
            if (enabled) return value;

            return fallback;
        }

        public ToggleValue() : this(default, false) { }
        public ToggleValue(TValue value) : this(value, true) { }
        public ToggleValue(TValue value, bool enabled)
        {
            this.enabled = enabled;
            this.value = value;
        }

        public static implicit operator ToggleValue<TValue>(TValue value) => new ToggleValue<TValue>(value);
    }
}