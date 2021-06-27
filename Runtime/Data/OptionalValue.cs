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
using UnityEngine.UIElements;

namespace MB
{
    /// <summary>
    /// A field that provides an optional value
    /// </summary>
    [Serializable]
    public abstract class OptionalValue
    {
        [SerializeField]
        protected bool enabled;
        public bool Enabled { get { return enabled; } }

#if UNITY_EDITOR
        public static bool ReadPref<TValue>(Component self, string id, OptionalValue<TValue> target)
        {
            var key = FormatPrefKey(self, id);

            if (EditorPrefs.HasKey(key) == false) return false;

            var type = typeof(TValue);

            if (type == typeof(float))
            {
                var value = EditorPrefs.GetFloat(key);
                target.Value = (TValue)(object)value;
                return true;
            }

            if (type == typeof(int))
            {
                var value = EditorPrefs.GetInt(key);
                target.Value = (TValue)(object)value;
                return true;
            }

            if (type == typeof(string))
            {
                var value = EditorPrefs.GetString(key);
                target.Value = (TValue)(object)value;
                return true;
            }

            if (type == typeof(bool))
            {
                var value = EditorPrefs.GetBool(key);
                target.Value = (TValue)(object)value;
                return true;
            }

            throw new NotImplementedException();
        }

        public static string FormatPrefKey(Component self, string id) => $"{self.GetType().FullName}/{id}";

        public static void SetPref<TValue>(Component self, string id, OptionalValue<TValue> target)
        {
            if (target.Enabled == false) return;

            var key = FormatPrefKey(self, id);
            var value = target.Value;

            EditorUtility.SetDirty(self);

            {
                if (value is float number)
                {
                    EditorPrefs.SetFloat(key, number);
                    return;
                }
            }
            {
                if (value is int number)
                {
                    EditorPrefs.SetFloat(key, number);
                    return;
                }
            }
            {
                if (value is string text)
                {
                    EditorPrefs.SetString(key, text);
                    return;
                }
            }
            {
                if (value is bool boolean)
                {
                    EditorPrefs.SetBool(key, boolean);
                    return;
                }
            }

            throw new NotImplementedException();
        }

        [CustomPropertyDrawer(typeof(OptionalValue), true)]
        public class BaseDrawer : PersistantPropertyDrawer
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
                if (value.isExpanded)
                    return EditorGUI.GetPropertyHeight(value, Label, true);

                return EditorGUIUtility.singleLineHeight;
            }

            public override void Draw(Rect rect)
            {
                rect = EditorGUI.IndentedRect(rect);
                EditorGUI.indentLevel = 0;

                DrawToggle(ref rect, Property, Label);

                GUI.enabled = enabled.boolValue;
                DrawValue(ref rect, Property, Label);
                GUI.enabled = true;
            }

            protected virtual void DrawToggle(ref Rect rect, SerializedProperty property, GUIContent label)
            {
                var size = EditorGUIUtility.singleLineHeight - 2f;

                enabled.boolValue = EditorGUI.Toggle(new Rect(rect.x, rect.y, size, size), enabled.boolValue);

                var offset = 5f;

                rect.x += size + offset;
                rect.width -= size + offset;
            }

            protected virtual void DrawValue(ref Rect rect, SerializedProperty property, GUIContent label)
            {
                var value = property.FindPropertyRelative("value");

                var labelWidth = GUI.skin.label.CalcSize(label).x;

                EditorGUIUtility.labelWidth = labelWidth + 5f;

                if (IsExpandControl(value))
                {
                    var offset = 10f;

                    rect.x += offset;
                    rect.width -= offset;

                    EditorGUIUtility.labelWidth += offset;
                }

                EditorGUI.PropertyField(rect, value, label, true);
            }

            protected virtual bool IsExpandControl(SerializedProperty target)
            {
                if (target.type.Contains("Vector")) return false;

                if (target.hasVisibleChildren == false) return false;

                return true;
            }
        }
#endif
    }

    [Serializable]
    public class OptionalValue<TValue> : OptionalValue
    {
        [SerializeField]
        protected TValue value;
        public TValue Value
        {
            get => value;
            set => this.value = value;
        }

        public static implicit operator TValue(OptionalValue<TValue> target) => target.value;

        public override string ToString() => $"{value}";

        public OptionalValue() : this(default, false) { }
        public OptionalValue(TValue value) : this(value, false) { }
        public OptionalValue(TValue value, bool enabled)
        {
            this.enabled = enabled;
            this.value = value;
        }

        public static implicit operator OptionalValue<TValue>(TValue value) => new OptionalValue<TValue>(value);
    }
}