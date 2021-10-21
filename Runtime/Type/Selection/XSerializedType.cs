using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using MB.ThirdParty;
#endif

namespace MB
{
    [Serializable]
    public abstract class XSerializedType
    {
        /// <summary>
        /// The reason for the inclusion of this accurately named unused variable is -as the name might suggest- the fact that
        /// Unity is indeed dumb, when using a custom data structure Unity by default takes the first fetched string
        /// Serialized property and uses it as a label for that array element, this behaviour is undersirable by me in this case,
        /// hence the unused empty variable
        /// </summary>
        [SerializeField]
        protected string UnityIsDumbVariable = string.Empty;

        [SerializeField]
        protected string id = default;
        public string ID => id;

        protected Type cache;
        protected bool cached;

        public virtual Type Type
        {
            get
            {
                if (cached == false)
                {
                    cache = IDToType(id);
                    cached = true;
                }

                return cache;
            }
            set
            {
                id = TypeToID(value);
                cached = false;
            }
        }

        public static Type IDToType(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            return Type.GetType(id);
        }
        public static string TypeToID(Type type)
        {
            if (type == null)
                return string.Empty;

            return type.AssemblyQualifiedName;
        }

        public virtual void OnBeforeSerialize()
        {
            cached = false;
        }
        public virtual void OnAfterDeserialize()
        {
            cached = false;
        }

        public override string ToString()
        {
            if (Type == null)
                return "null";

            return Type.ToString();
        }

        public XSerializedType() : this(default) { }
        public XSerializedType(Type type)
        {
            this.Type = type;
        }

#if UNITY_EDITOR
        protected abstract class BaseDrawer<THandler> : PropertyDrawer
            where THandler : class
        {
            public abstract Type IgnoreAttributeType { get; }

            public void GetMetadata(out Type argument, out SerializedTypeInclude includes)
            {
                var attribute = base.attribute as BaseXSerializedTypeSelectionAttribute;

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

            public SerializedProperty GetIDProperty(SerializedProperty property) => property.FindPropertyRelative("id");

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
            {
                GetMetadata(out var argument, out var includes);

                var id = GetIDProperty(property);
                var selection = IDToType(id.stringValue);

                if (ValidateType(argument, selection, includes))
                {
                    EditorGUI.BeginProperty(rect, label, property);

                    rect = EditorGUI.PrefixLabel(rect, label);

                    if (EditorGUI.DropdownButton(rect, FormatDisplayContent(selection), FocusType.Keyboard, Styles.DropdownButton))
                    {
                        var types = Query(argument);
                        types = Filter(types, argument, includes);

                        var handler = RetrieveHandler(property, selection);
                        SearchablePopup<THandler>.Show(rect, types, handler, FormatDisplayContentOfHandler, OnSelect, includeNone: true);
                        void OnSelect(THandler handler)
                        {
                            var type = HandlerToType(handler);
                            id.LateModifyProperty(x => x.stringValue = TypeToID(type));
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

            public abstract THandler RetrieveHandler(SerializedProperty property, Type selection);

            public abstract Type HandlerToType(THandler handler);

            public virtual bool ValidateType(Type argument, Type type, SerializedTypeInclude includes)
            {
                if (type == null) return true;

                if (includes.HasFlag(SerializedTypeInclude.Self) == false && type == argument) return false;
                if (includes.HasFlag(SerializedTypeInclude.Abstract) == false && type.IsAbstract) return false;
                if (includes.HasFlag(SerializedTypeInclude.Generic) == false && type.IsGenericType) return false;

                if (argument.IsAssignableFrom(type) == false) return false;
                if (type.GetCustomAttribute(IgnoreAttributeType) != null) return false;

                return true;
            }

            public virtual GUIContent FormatDisplayContentOfHandler(THandler handler)
            {
                var type = HandlerToType(handler);
                return FormatDisplayContent(type);
            }
            public virtual GUIContent FormatDisplayContent(Type type)
            {
                if (type == null)
                    return new GUIContent("None");

                var name = type.Name.ToPrettyString();

                var text = $"{name} <color=#A4A4A4>({type.Namespace})</color>";
                var tooltip = $"{name} ({type.Namespace})";

                return new GUIContent(text, tooltip);
            }

            public virtual List<THandler> Filter(List<THandler> list, Type argument, SerializedTypeInclude includes)
            {
                list.RemoveAll(IsInvalid);
                bool IsInvalid(THandler handler)
                {
                    var type = HandlerToType(handler);

                    if (ValidateType(argument, type, includes) == false)
                        return true;

                    return false;
                }

                return list;
            }

            public abstract List<THandler> Query(Type argument);

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

        //Static Utility

        public static class Defaults
        {
            public const SerializedTypeInclude Include = SerializedTypeInclude.None;
        }

        public static implicit operator Type(XSerializedType target) => target.Type;
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public abstract class BaseXSerializedTypeSelectionAttribute : PropertyAttribute
    {
        public Type Argument { get; }
        public SerializedTypeInclude Includes { get; }

        public BaseXSerializedTypeSelectionAttribute(Type argument) : this(argument, XSerializedType.Defaults.Include) { }
        public BaseXSerializedTypeSelectionAttribute(Type argument, SerializedTypeInclude includes)
        {
            this.Argument = argument;
            this.Includes = includes;
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public abstract class BaseXSerializedTypeIgnoreAttribute : Attribute { }

    [Flags]
    public enum SerializedTypeInclude
    {
        None = 0,

        Self = 1 << 0,
        Abstract = 1 << 1,
        Generic = 1 << 2,

        All = Self | Abstract | Generic,
    }
}