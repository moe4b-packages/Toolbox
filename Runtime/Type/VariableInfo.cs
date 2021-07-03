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
        public PropertyInfo Property { get; protected set; }
        public bool IsProperty => Property != null;

        public FieldInfo Field { get; protected set; }
        public bool IsField => Field != null;

        public MemberInfo Member
        {
            get
            {
                if (IsField)
                    return Field;

                if (IsProperty)
                    return Property;

                throw new NotImplementedException();
            }
        }

        public Type DeclaringType => Member.DeclaringType;

        public Type ValueType
        {
            get
            {
                if (IsField)
                    return Field.FieldType;

                if (IsProperty)
                    return Property.PropertyType;

                throw new NotImplementedException();
            }
        }

        public object Read(object target)
        {
            if (IsProperty)
                return Property.GetValue(target);

            if (IsField)
                return Field.GetValue(target);

            throw new NotImplementedException();
        }

        public void Set(object target, object value)
        {
            if (IsProperty)
            {
                Property.SetValue(target, value);
                return;
            }

            if (IsField)
            {
                Field.SetValue(target, value);
                return;
            }

            throw new NotImplementedException();
        }

        public override string ToString() => Member.ToString();

        public VariableInfo(PropertyInfo property)
        {
            this.Property = property;
        }
        public VariableInfo(FieldInfo field)
        {
            this.Field = field;
        }

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
    }

    public static class VariableInfoExtensions
    {
        public static List<VariableInfo> GetVariables(this Type type, BindingFlags flags)
        {
            var list = new List<VariableInfo>();

            var fields = type.GetFields(flags);
            for (int i = 0; i < fields.Length; i++)
            {
                var variable = new VariableInfo(fields[i]);
                list.Add(variable);
            }

            var properties = type.GetProperties(flags);
            for (int i = 0; i < properties.Length; i++)
            {
                var variable = new VariableInfo(properties[i]);
                list.Add(variable);
            }

            return list;
        }
    }
}