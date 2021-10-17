using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using MB.ThirdParty;
#endif

using System.Reflection;

namespace MB
{
    /// <summary>
    /// Provides a Unity serializable type field,
    /// can be restricted to use specific derived types using the nested SelectionAttribute
    /// </summary>
    [Serializable]
    public class SerializedType : ISerializationCallbackReceiver
    {
        [SerializeField]
        string id = default;
        public string ID => id;

        Type cache;
        bool cached;

        public Type Type
        {
            get
            {
                if (cached == false)
                {
                    cache = Convert(id);
                    cached = true;
                }

                return cache;
            }
            set
            {
                id = Convert(value);
            }
        }

        public static Type Convert(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            return Type.GetType(id);
        }
        public static string Convert(Type type)
        {
            if (type == null)
                return string.Empty;

            return type.AssemblyQualifiedName;
        }

        public void OnBeforeSerialize()
        {
            cached = false;
        }
        public void OnAfterDeserialize()
        {
            cached = false;
        }

        public override string ToString()
        {
            if (Type == null)
                return "null";

            return Type.ToString();
        }

        public static implicit operator Type(SerializedType target) => target.Type;

        public SerializedType() : this(null) { }
        public SerializedType(Type type)
        {
            this.Type = type;
        }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(SerializedType))]
        [CustomPropertyDrawer(typeof(SelectionAttribute))]
        class Drawer : PropertyDrawer
        {
            public void GetMetadata(out Type argument)
            {
                var attribute = base.attribute as SelectionAttribute;

                if (attribute == null)
                {
                    argument = typeof(MonoBehaviour);
                }
                else
                {
                    argument = attribute.Argument;
                }
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
            {
                GetMetadata(out var argument);

                var id = property.FindPropertyRelative("id");
                var selection = Convert(id.stringValue);

                if(ValidateType(argument, selection))
                {
                    rect = EditorGUI.PrefixLabel(rect, label);

                    var content = new GUIContent(FormatDisplayName(selection));

                    if (EditorGUI.DropdownButton(rect, content, FocusType.Keyboard))
                    {
                        var types = Query(argument);
                        var names = types.Select(FormatDisplayName);

                        var index = selection == null ? -1 : types.IndexOf(selection);

                        SearchablePopup.Show(rect, index, names, OnSelect, includeNone: true);
                        void OnSelect(int index)
                        {
                            id.LateModifyProperty(Process);
                            void Process(SerializedProperty property)
                            {
                                if (index == -1)
                                    property.stringValue = string.Empty;
                                else
                                    property.stringValue = Convert(types[index]);
                            }
                        }
                    }
                }
                else
                {
                    var area = MUtility.GUICoordinates.SliceHorizontalPercentage(ref rect, 80f);
                    EditorGUI.HelpBox(area, $" Invalid Selection of {FormatDisplayName(selection)}", MessageType.Error);

                    rect.xMin += 2;

                    if (GUI.Button(rect, "Clear"))
                        id.stringValue = string.Empty;
                }
            }

            public static string FormatDisplayName(Type type)
            {
                if (type == null)
                    return "None";

                return type.FullName;
            }

            public static bool ValidateType(Type argument, Type type)
            {
                if (type == null) return true;

                if (argument.IsAssignableFrom(type) == false)
                    return false;

                if (IgnoreAttribute.IsDefined(type))
                    return false;

                return true;
            }

            public static IList<Type> Query(Type argument)
            {
                var list = new List<Type>();

                foreach (var type in TypeCache.GetTypesDerivedFrom(argument))
                {
                    if (ValidateType(argument, type))
                        list.Add(type);
                }

                return list;
            }
        }
#endif

        [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
        public class SelectionAttribute : PropertyAttribute
        {
            public Type Argument { get; }

            public SelectionAttribute(Type argument)
            {
                this.Argument = argument;
            }
        }

        /// <summary>
		/// Attribute that ignores the specified class from selection
		/// </summary>
		[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
        public sealed class IgnoreAttribute : Attribute
        {
#if UNITY_EDITOR
            public static bool IsDefined(MonoScript script)
            {
                var type = script.GetClass();

                return IsDefined(type);
            }
            public static bool IsDefined(Type type)
            {
                var attribute = type.GetCustomAttribute<IgnoreAttribute>();

                return attribute != null;
            }
#endif
        }
    }
}