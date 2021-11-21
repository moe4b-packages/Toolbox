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
    public class SerializedType : XSerializedType
    {
        public static implicit operator SerializedType(Type type) => new SerializedType(type);

        public SerializedType() : base(default) { }
        public SerializedType(Type type) : base(type) { }

        public static SerializedType From<T>()
        {
            var type = typeof(T);

            return new SerializedType(type);
        }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(SerializedType))]
        [CustomPropertyDrawer(typeof(SelectionAttribute))]
        class Drawer : BaseDrawer<Type>
        {
            public override Type IgnoreAttributeType => typeof(IgnoreAttribute);

            public override Type RetrieveHandler(SerializedProperty property, Type selection)
            {
                return selection;
            }

            public override Type HandlerToType(Type handler)
            {
                return handler;
            }

            public override List<Type> Query(Type argument)
            {
                var list = new List<Type> { argument };

                list.AddRange(TypeCache.GetTypesDerivedFrom(argument));

                return list;
            }
        }
#endif

        [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
        public class SelectionAttribute : BaseXSerializedTypeSelectionAttribute
        {
            public SelectionAttribute(Type argument) : base(argument) { }
            public SelectionAttribute(Type argument, SerializedTypeInclude includes) : base(argument, includes) { }
        }

        [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
        public class IgnoreAttribute : BaseXSerializedTypeIgnoreAttribute { }
    }
}