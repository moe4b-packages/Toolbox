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
    /// <summary>
    /// Attribute to apply to single/list of component fields to auto retrive them on ResolveAll
    /// </summary>
    public static class AutoDependency
    {
        public static void ResolveAll(UObjectSurrogate surrogate)
        {
            using (ComponentQuery.Collection.NonAlloc.InHierarchy<MonoBehaviour>(surrogate, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                    ResolveComponent(list[i]);
            }
        }

        static void ResolveComponent(Component component)
        {
            var elements = TypeInfo.Query(component);

            for (int i = 0; i < elements.Count; i++)
                ResolveElement(component, elements[i]);
        }

        static void ResolveElement(Component component, Element element)
        {
            if (element.IsList)
            {
                using (DisposablePool.List<Component>.Lease(out var list))
                {
                    for (int i = 0; i < element.Scopes.Length; i++)
                        using (ComponentQuery.Collection.NonAlloc.In(component, element.DependencyType, out var temp, ConvertScope(element.Scopes[i])))
                            list.AddRange(temp);

                    element.SetElements(component, list);
                }
            }
            else
            {
                for (int i = 0; i < element.Scopes.Length; i++)
                {
                    var target = ComponentQuery.Single.In(component, element.DependencyType, ConvertScope(element.Scopes[i]));

                    if (target != null)
                    {
                        element.SetValue(component, target);
                        break;
                    }
                }
            }
        }

        public enum Scope
        {
            Self = ComponentQueryScope.Self,
            Children = ComponentQueryScope.Children,
            Hierarchy = ComponentQueryScope.Hierarchy,
            Parents = ComponentQueryScope.Parents,
            Scene = ComponentQueryScope.Scene,
            Global = ComponentQueryScope.Global
        }

        public const Scope Self = Scope.Self;
        public const Scope Children = Scope.Children;
        public const Scope Hierarchy = Scope.Hierarchy;
        public const Scope Parents = Scope.Parents;
        public const Scope Scene = Scope.Scene;
        public const Scope Global = Scope.Global;

        public static ComponentQueryScope ConvertScope(Scope scope) => (ComponentQueryScope)(int)scope;
        public static Scope ConvertScope(ComponentQueryScope scope) => (Scope)(int)scope;

        public class Element
        {
            public AutoDependencyAttribute Attribute { get; protected set; }
            public Scope[] Scopes => Attribute.Scopes;

            public PropertyInfo Property { get; protected set; }

            public Type DependencyType { get; protected set; }

            public bool IsList { get; protected set; }

            public void SetValue(Component source, object value) => Property.SetValue(source, value);

            public void SetElements(Component source, IList<Component> elements)
            {
                var list = Activator.CreateInstance(Property.PropertyType, elements.Count) as IList;

                for (int i = 0; i < elements.Count; i++)
                    list.Add(elements[i]);

                SetValue(source, list);
            }

            public Element(AutoDependencyAttribute attribute, PropertyInfo property)
            {
                this.Attribute = attribute;
                this.Property = property;

                IsList = CheckIsList(Property.PropertyType);

                if (IsList)
                    DependencyType = Property.PropertyType.GetGenericArguments()[0];
                else
                    DependencyType = Property.PropertyType;
            }

            public static bool TryParse(PropertyInfo property, out Element element)
            {
                var attribute = property.GetCustomAttribute<AutoDependencyAttribute>();

                if (attribute == null)
                {
                    element = null;
                    return false;
                }
                else
                {
                    element = new Element(attribute, property);
                    return true;
                }
            }

            static bool CheckIsList(Type type)
            {
                if (type.IsGenericType == false) return false;

                if (type.GetGenericTypeDefinition() != typeof(List<>)) return false;

                return true;
            }
        }

        public static class TypeInfo
        {
            public static Dictionary<Type, List> Cache { get; private set; }

            public static BindingFlags BindingFlags { get; private set; } = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            public class List : List<Element> { }

            public static List Query(Component component)
            {
                var type = component.GetType();

                return Query(type);
            }
            public static List Query(Type type)
            {
                if (Cache.TryGetValue(type, out var list))
                    return list;

                list = new List();

                foreach (var property in type.GetProperties())
                {
                    if (Element.TryParse(property, out var element) == false) continue;

                    list.Add(element);
                }

                Cache[type] = list;

                return list;
            }

            static TypeInfo()
            {
                Cache = new Dictionary<Type, List>();
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class AutoDependencyAttribute : Attribute
    {
        public AutoDependency.Scope[] Scopes { get; private set; }

        public AutoDependencyAttribute() : this(AutoDependency.Scope.Children) { }
        public AutoDependencyAttribute(params AutoDependency.Scope[] scopes)
        {
            this.Scopes = scopes;
        }

        public static bool IsDefined(PropertyInfo property) => property.IsDefined(typeof(AutoDependencyAttribute), true);
    }
}