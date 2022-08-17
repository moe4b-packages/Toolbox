using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEditor;

using UnityEngine;

using Object = UnityEngine.Object;

namespace MB
{
    [Serializable]
    public class SerializedVariable
    {
        [SerializeField]
        Object context;
        public Object Context => context;

        [SerializeField]
        Object target;
        public Object Target => target;

        [SerializeField]
        string id;
        public string ID => id;

        [SerializeField]
        ImplementationType implementation;
        public ImplementationType Implementation => implementation;
        public enum ImplementationType
        {
            Field, Property
        }

        public bool IsAssigned
        {
            get
            {
                if (context == null || target == null)
                    return false;

                if (string.IsNullOrEmpty(id))
                    return false;

                return true;
            }
        }

        public VariableInfo Load() => Load(target, id, implementation);

        public SerializedVariable(Object target, string id, ImplementationType implementation)
        {
            if (target is Component component)
                context = component.gameObject;
            else
                context = target;

            this.id = id;
            this.implementation = implementation;
        }

        #region Drawer
#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(SelectionAttribute))]
        [CustomPropertyDrawer(typeof(SerializedVariable))]
        public class Drawer : PropertyDrawer
        {
            public void GetMetadata(out Scope scope, out bool local)
            {
                var attribute = base.attribute as SelectionAttribute;

                if (attribute == null)
                {
                    scope = Scope.All;
                    local = false;
                }
                else
                {
                    scope = attribute.Scope;
                    local = attribute.Local;
                }
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                if (property.IsEditingMultipleObjects())
                    return EditorGUIUtility.singleLineHeight;

                GetMetadata(out var scope, out var local);

                var context = property.FindPropertyRelative(nameof(SerializedVariable.context));

                if (context.objectReferenceValue == null || local)
                    return EditorGUIUtility.singleLineHeight;
                else
                    return EditorGUIUtility.singleLineHeight * 2f;
            }

            public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
            {
                if (property.IsEditingMultipleObjects())
                {
                    EditorGUI.HelpBox(rect, "Cannot Edit Multiple Variable Objects", MessageType.Info);
                    return;
                }

                GetMetadata(out var scope, out var local);

                var context = property.FindPropertyRelative(nameof(SerializedVariable.context));
                var target = property.FindPropertyRelative(nameof(SerializedVariable.target));
                var id = property.FindPropertyRelative(nameof(SerializedVariable.id));
                var implementation = property.FindPropertyRelative(nameof(SerializedVariable.implementation));

                EditorGUI.BeginProperty(rect, label, property);

                //GameObject Field
                {
                    if (local)
                    {
                        context.objectReferenceValue = (property.serializedObject.targetObject as Component).gameObject;
                    }
                    else
                    {
                        var area = MUtility.GUI.SliceLine(ref rect);

                        EditorGUI.BeginChangeCheck();
                        EditorGUI.PropertyField(area, context, label);
                        if (EditorGUI.EndChangeCheck())
                        {
                            target.objectReferenceValue = null;
                            id.stringValue = string.Empty;
                            implementation.intValue = 0;
                        }
                    }
                }

                //Dropdown
                if (context.objectReferenceValue != null)
                {
                    var area = MUtility.GUI.SliceLine(ref rect);

                    if (local)
                        area = EditorGUI.PrefixLabel(area, label);

                    var text = FormatLabel(context.objectReferenceValue, target.objectReferenceValue, id.stringValue);
                    var content = new GUIContent(text);

                    if (EditorGUI.DropdownButton(area, content, FocusType.Keyboard))
                    {
                        var selection = new Entry(context.objectReferenceValue, target.objectReferenceValue, id.stringValue, (ImplementationType)implementation.intValue);

                        Dropdown.Show(area, context.objectReferenceValue, selection, Handler);

                        void Handler(Entry entry)
                        {
                            target.objectReferenceValue = entry.Target;
                            id.stringValue = entry.Name;
                            implementation.intValue = (int)entry.Implementation;

                            target.serializedObject.ApplyModifiedProperties();
                        }
                    }
                }

                EditorGUI.EndProperty();
            }
        }

        public static class Dropdown
        {
            public delegate void HandlerDelegate(Entry entry);

            public static void Show(Rect rect, Object context, Entry selection, HandlerDelegate handler)
            {
                var menu = new GenericMenu();

                if (context is GameObject gameObject)
                {
                    Register(menu, context, context, selection, Callback, false);

                    var components = gameObject.GetComponents<Component>();
                    for (int i = 0; i < components.Length; i++)
                        Register(menu, context, components[i], selection, Callback, false);
                }
                else
                {
                    Register(menu, context, context, selection, Callback, true);
                }

                menu.AddSeparator("");

                menu.AddItem("None", selection == default, Callback, default(Entry));

                menu.DropDown(rect);

                void Callback(object value)
                {
                    var selection = (Entry)value;

                    handler.Invoke(selection);
                }
            }

            static void Register(GenericMenu menu, Object context, Object target, Entry selection, GenericMenu.MenuFunction2 callback, bool root)
            {
                var type = target.GetType();

                if (TypeAnalyzer.TryIterate(type, out var variables) == false)
                    return;

                foreach (var variable in variables)
                {
                    var implementation = GetImplementationType(variable);

                    var entry = new Entry(context, target, variable.Member.Name, implementation);

                    string text;

                    if (root)
                        text = $"{variable.Name} ({FormatTypeName(variable.ValueType)})";
                    else
                        text = $"{type.Name}/{variable.Name} ({FormatTypeName(variable.ValueType)})";

                    var content = new GUIContent(text);

                    var isOn = entry == selection;

                    menu.AddItem(content, isOn, callback, entry);
                }
            }

            static ImplementationType GetImplementationType(VariableInfo variable)
            {
                switch (variable.Implementation)
                {
                    case VariableInfo.ImplementationType.Field:
                        return ImplementationType.Field;

                    case VariableInfo.ImplementationType.Property:
                        return ImplementationType.Property;
                }

                throw new ArgumentException();
            }
        }

        public readonly struct Entry
        {
            public readonly Object Context { get; }
            public readonly Object Target { get; }
            public readonly string Name { get; }
            public readonly ImplementationType Implementation { get; }

            public override bool Equals(object obj)
            {
                if (obj is Entry target)
                    return Equals(target);

                return false;
            }
            public bool Equals(Entry target)
            {
                if (this.Target != target.Target) return false;
                if (this.Name != target.Name) return false;
                if (this.Implementation != target.Implementation) return false;

                return true;
            }

            public override int GetHashCode() => (Target, Name, Implementation).GetHashCode();

            public readonly string TextCache { get; }
            public override string ToString() => TextCache;

            public Entry(Object context, Object target, string name, ImplementationType implementation)
            {
                this.Context = context;
                this.Target = target;
                this.Name = name;
                this.Implementation = implementation;

                TextCache = FormatLabel(Context, Target, Name);
            }

            public static bool operator ==(Entry right, Entry left) => right.Equals(left);
            public static bool operator !=(Entry right, Entry left) => right.Equals(left);
        }
#endif
        #endregion

        #region Attributes
        [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
        public class SelectionAttribute : PropertyAttribute
        {
            /// <summary>
            /// Set to true to have the variable selection limited to the gameObject which the component that this field resides on
            /// </summary>
            public bool Local { get; set; } = false;

            public Scope Scope { get; }

            public SelectionAttribute() : this(Scope.All) { }
            public SelectionAttribute(Scope scope)
            {
                this.Scope = scope;
            }
        }

        [Flags]
        public enum Scope
        {
            Fields = 1 << 0,
            Properties = 1 << 1,

            All = ~1,
        }

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
        public class IgnoreAttribute : PropertyAttribute
        {

        }
        #endregion

        #region Static Utility
        public const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

        public static VariableInfo Load(Object target, string name, ImplementationType implementation)
        {
            var type = target.GetType();

            switch (implementation)
            {
                case ImplementationType.Field:
                    var field = type.GetField(name, Flags);
                    return new VariableInfo(field);

                case ImplementationType.Property:
                    var property = type.GetProperty(name, Flags);
                    return new VariableInfo(property);
            }

            throw new ArgumentOutOfRangeException(nameof(implementation));
        }

        public static string FormatLabel(Object context, Object target, string name)
        {
            if (target == null || name == "")
                return "None";

            if (context is GameObject)
                return $"{target.GetType().Name} -> {name}";
            else
                return $"{name}";
        }

#if UNITY_EDITOR
        public static class TypeAnalyzer
        {
            public static Dictionary<Type, List<VariableInfo>> Cache;

            public static bool TryIterate(Type type, out List<VariableInfo> collection)
            {
                if (type.GetCustomAttribute<IgnoreAttribute>() != null)
                {
                    collection = default;
                    return false;
                }

                if (Cache.TryGetValue(type, out collection))
                    return true;

                var fields = type.GetFields(Flags);
                var properties = type.GetProperties(Flags);

                collection = new List<VariableInfo>(fields.Length + properties.Length);

                for (int i = 0; i < fields.Length; i++)
                {
                    if (ValidateField(fields[i]) == false)
                        continue;

                    var variable = new VariableInfo(fields[i]);
                    collection.Add(variable);
                }

                for (int i = 0; i < properties.Length; i++)
                {
                    if (ValidateProperty(properties[i]) == false)
                        continue;

                    var variable = new VariableInfo(properties[i]);
                    collection.Add(variable);
                }

                collection.Sort((x, y) => x.Name.CompareTo(y.Name));

                Cache.Add(type, collection);

                return true;

                static bool ValidateField(FieldInfo field)
                {
                    if (field.GetCustomAttribute<IgnoreAttribute>() != null) return false;

                    return true;
                }
                static bool ValidateProperty(PropertyInfo property)
                {
                    if (property.GetCustomAttribute<IgnoreAttribute>() != null) return false;

                    if (property.GetMethod == null) return false;
                    if (property.GetMethod.IsPublic == false) return false;

                    if (property.SetMethod == null) return false;
                    if (property.SetMethod.IsPublic == false) return false;

                    return true;
                }
            }

            static TypeAnalyzer()
            {
                Cache = new Dictionary<Type, List<VariableInfo>>();
            }
        }

        public static string FormatTypeName(Type type)
        {
            if (type == typeof(float))
                return "Float";

            if (type == typeof(double))
                return "Double";

            if (type == typeof(int))
                return "Int";

            if (type == typeof(short))
                return "Short";

            if (type == typeof(long))
                return "Long";

            if (type == typeof(bool))
                return "Bool";

            return type.Name;
        }
#endif
        #endregion
    }
}