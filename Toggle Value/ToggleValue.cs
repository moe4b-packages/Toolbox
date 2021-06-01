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
        public class Drawer : PersistantPropertyDrawer
        {
            protected SerializedProperty enabled;
            protected SerializedProperty value;

            protected override void Init()
            {
                base.Init();

                enabled = Property.FindPropertyRelative(nameof(enabled));
                value = Property.FindPropertyRelative(nameof(value));
            }

            public override float CalculateHeight()
            {
                if (enabled.boolValue && value.isExpanded)
                    return EditorGUI.GetPropertyHeight(value, Label, true);

                return EditorGUIUtility.singleLineHeight;
            }

            public override void Draw(Rect rect)
            {
                rect = EditorGUI.IndentedRect(rect);
                EditorGUI.indentLevel = 0;

                DrawToggle(ref rect, Property, Label);

                if (enabled.boolValue)
                    DrawValue(ref rect, Property, Label);
                else
                    DrawLabel(ref rect, Property, Label);
            }

            protected virtual void DrawToggle(ref Rect rect, SerializedProperty property, GUIContent label)
            {
                var size = EditorGUIUtility.singleLineHeight - 2f;

                enabled.boolValue = EditorGUI.Toggle(new Rect(rect.x, rect.y, size, size), enabled.boolValue);

                var offset = 5f;

                rect.x += size + offset;
                rect.width -= size + offset;
            }

            protected virtual void DrawLabel(ref Rect rect, SerializedProperty property, GUIContent label)
            {
                var width = GUI.skin.label.CalcSize(label).x + 10f;

                EditorGUI.LabelField(new Rect(rect.x, rect.y, width, rect.height), label);

                rect.x += width;
                rect.width -= width;
            }

            protected virtual void DrawValue(ref Rect rect, SerializedProperty property, GUIContent label)
            {
                var value = property.FindPropertyRelative("value");

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

        public virtual TValue Evaluate(TValue defaultValue)
        {
            if (enabled) return value;

            return defaultValue;
        }

        public ToggleValue() : this(false, default(TValue))
        {

        }
        public ToggleValue(bool enabled, TValue value)
        {
            this.enabled = enabled;

            this.value = value;
        }

        public static implicit operator ToggleValue<TValue>(TValue value) => new ToggleValue<TValue>(true, value);
    }

    #region Defaults
    [Serializable]
    public class IntToggleValue : ToggleValue<int> { }

    [Serializable]
    public class FloatToggleValue : ToggleValue<float> { }

    [Serializable]
    public class BoolToggleValue : ToggleValue<bool> { }

    [Serializable]
    public class StringToggleValue : ToggleValue<string> { }

    [Serializable]
    public class ColorToggleValue : ToggleValue<Color>
    {
        public ColorToggleValue()
        {
            value = Color.white;
        }
    }

    [Serializable]
    public class Vector2ToggleValue : ToggleValue<Vector2> { }

    [Serializable]
    public class Vector3ToggleValue : ToggleValue<Vector3> { }
    #endregion
}