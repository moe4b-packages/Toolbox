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
    /// A utility for querying types all types in all assemblies
    /// </summary>
	public static class TypeQuery
    {
        public static List<Type> Collection { get; private set; }

        public static List<Type> FindAll<T>()
        {
            var type = typeof(T);

            return FindAll(type);
        }
        public static List<Type> FindAll(Type target)
        {
            return FindAll(Predicate);

            bool Predicate(Type type)
            {
                if (target.IsAssignableFrom(type) == false) return false;

                return true;
            }
        }

        public static List<Type> FindAll(Predicate<Type> predicate)
        {
            var list = new List<Type>();

            for (int i = 0; i < Collection.Count; i++)
                if (predicate(Collection[i]))
                    list.Add(Collection[i]);

            return list;
        }

        static TypeQuery()
        {
            Collection = new List<Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                foreach (var type in assembly.GetTypes())
                    Collection.Add(type);
        }
    }
}