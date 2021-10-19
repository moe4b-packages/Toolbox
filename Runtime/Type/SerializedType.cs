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
        /// <summary>
        /// The reason for the inclusion of this accurately named unused variable is -as the name might suggest- the fact that
        /// Unity is indeed dumb, when using a custom data structure Unity by default takes the first fetched string
        /// Serialized property and uses it as a label for that array element, this behaviour is undersirable by me in this case,
        /// hence the unused empty variable
        /// </summary>
        [SerializeField]
        string UnityIsDumbVariable = string.Empty;

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
            public void GetMetadata(out Type argument, out Includes includes)
            {
                var attribute = base.attribute as SelectionAttribute;

                if (attribute == null)
                {
                    argument = typeof(MonoBehaviour);
                    includes = Defaults.Include;
                }
                else
                {
                    argument = attribute.Argument;
                    includes = attribute.Includes;
                }
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
            {
                GetMetadata(out var argument, out var includes);

                var id = property.FindPropertyRelative("id");
                var selection = Convert(id.stringValue);

                if(ValidateType(argument, selection, includes))
                {
                    EditorGUI.BeginProperty(rect, label, property);

                    rect = EditorGUI.PrefixLabel(rect, label);

                    if (EditorGUI.DropdownButton(rect, FormatDisplayContent(selection), FocusType.Keyboard, Styles.DropdownButton))
                    {
                        var types = Query(argument, includes);

                        SearchablePopup<Type>.Show(rect, types, selection, FormatDisplayContent, OnSelect, includeNone: true);
                        void OnSelect(Type type)
                        {
                            id.LateModifyProperty(x => x.stringValue = Convert(type));
                        }
                    }

                    EditorGUI.EndProperty();
                }
                else
                {
                    var area = MUtility.GUICoordinates.SliceHorizontalPercentage(ref rect, 80f);
                    EditorGUI.HelpBox(area, $" Invalid Selection of {selection.Name}", MessageType.Error);

                    rect.xMin += 2;

                    if (GUI.Button(rect, "Clear"))
                        id.stringValue = string.Empty;
                }
            }

            public static GUIContent FormatDisplayContent(Type type)
            {
                if (type == null)
                    return new GUIContent("None");

                var text = $"{type.Name} <color=#A4A4A4>({type.Namespace})</color>";
                var tooltip = $"{type.Name} ({type.Namespace})";

                return new GUIContent(text, tooltip);
            }

            public static bool ValidateType(Type argument, Type type, Includes includes)
            {
                if (type == null) return true;

                if (includes.HasFlag(Includes.Self) == false && type == argument) return false;
                if (includes.HasFlag(Includes.Abstract) == false && type.IsAbstract) return false;
                if (includes.HasFlag(Includes.Generic) == false && type.IsGenericType) return false;

                if (argument.IsAssignableFrom(type) == false) return false;
                if (IgnoreAttribute.IsDefined(type)) return false;

                return true;
            }

            public static IList<Type> Query(Type argument, Includes includes)
            {
                var list = new List<Type>();

                if (ValidateType(argument, argument, includes))
                    list.Add(argument);

                foreach (var type in TypeCache.GetTypesDerivedFrom(argument))
                {
                    if (ValidateType(argument, type, includes))
                        list.Add(type);
                }

                return list;
            }

            public static class Styles
            {
                public static GUIStyle DropdownButton;

                static Styles()
                {
                    DropdownButton = EditorStyles.miniPullDown;
                    DropdownButton.richText = true;
                }
            }
        }
#endif

        [Flags]
        public enum Includes
        {
            None = 0,

            Self = 1 << 0,
            Abstract = 1 << 1,
            Generic = 1 << 2,

            All = Self | Abstract | Generic,
        }

        public static class Defaults
        {
            public const Includes Include = Includes.None;
        }

        [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
        public class SelectionAttribute : PropertyAttribute
        {
            public Type Argument { get; }
            public Includes Includes { get; set; } = Defaults.Include;

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