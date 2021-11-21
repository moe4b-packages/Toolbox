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
    public class VariableInfo
    {
        public PropertyInfo Property { get; }
        public FieldInfo Field { get; }

        public ImplementationType Implementation { get; }
        public enum ImplementationType
        {
            None, Field, Property
        }

        public MemberInfo Member { get; }
        public string Name => Member.Name;
        public Type DeclaringType => Member.DeclaringType;

        public Type ValueType { get; }

        public object Read(object target)
        {
            switch (Implementation)
            {
                case ImplementationType.Field:
                    return Field.GetValue(target);

                case ImplementationType.Property:
                    return Property.GetValue(target);

                default:
                    throw new InvalidOperationException();
            }
        }
        public void Set(object target, object value)
        {
            switch (Implementation)
            {
                case ImplementationType.Field:
                    Field.SetValue(target, value);
                    break;

                case ImplementationType.Property:
                    Property.SetValue(target, value);
                    break;

                default:
                    throw new InvalidOperationException();
            }
        }

        public override string ToString() => Member.ToString();

        protected VariableInfo(FieldInfo field, PropertyInfo property)
        {
            if (field != null)
            {
                this.Field = field;
                Member = field;
                Implementation = ImplementationType.Field;
                ValueType = field.FieldType;
            }
            else if (property != null)
            {
                this.Property = property;
                Member = property;
                Implementation = ImplementationType.Property;
                ValueType = property.PropertyType;
            }
            else
            {
                Implementation = ImplementationType.None;
            }
        }
        public VariableInfo(PropertyInfo property) : this(default, property) { }
        public VariableInfo(FieldInfo field) : this(field, default) { }

        public static VariableInfo From(MemberInfo member)
        {
            switch (member)
            {
                case FieldInfo field:
                    return new VariableInfo(field);

                case PropertyInfo property:
                    return new VariableInfo(property);

                default:
                    throw new NotImplementedException();
            }
        }

        public static VariableInfo Find(Type target, string id, ImplementationType implementation, BindingFlags flags)
        {
            switch (implementation)
            {
                case ImplementationType.Field:
                    var field = target.GetField(id, flags);
                    return new VariableInfo(field);

                case ImplementationType.Property:
                    var property = target.GetProperty(id, flags);
                    return new VariableInfo(property);
            }

            throw new ArgumentOutOfRangeException(nameof(implementation));
        }
    }

    public static class VariableInfoExtensions
    {
        public static List<VariableInfo> GetVariables(this Type type, BindingFlags flags)
        {
            var fields = type.GetFields(flags);
            var properties = type.GetProperties(flags);

            var list = new List<VariableInfo>(fields.Length + properties.Length);

            for (int i = 0; i < fields.Length; i++)
            {
                var variable = new VariableInfo(fields[i]);
                list.Add(variable);
            }

            for (int i = 0; i < properties.Length; i++)
            {
                var variable = new VariableInfo(properties[i]);
                list.Add(variable);
            }

            return list;
        }
    }
}