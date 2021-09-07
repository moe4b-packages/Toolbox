using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
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
    [Serializable]
    public abstract class UHashSet : UCollection
    {
#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(UHashSet), true)]
        public class Drawer : BaseDrawer
        {
            public const float KeyInfoContextWidth = 20f;

            protected override SerializedProperty FindListProperty(SerializedProperty property)
            {
                return property.FindPropertyRelative("list");
            }

            public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
            {
                EditorGUIUtility.labelWidth = 120f;

                base.OnGUI(rect, property, label);
            }
        }
#endif
    }

    [Serializable]
    public class UHashSet<T> : UHashSet, ISet<T>
    {
        [SerializeField]
        List<T> list;
        public List<T> List => list;

        public override int Count => list.Count;

        public bool IsReadOnly => false;

        HashSet<T> cache;
        public bool Cached => cache != null;
        public HashSet<T> HashSet
        {
            get
            {
                if (cache == null)
                {
                    cache = new HashSet<T>();

                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i] == null) continue;

                        cache.Add(list[i]);
                    }
                }

                return cache;
            }
        }

        public bool Contains(T item) => HashSet.Contains(item);

        public bool Add(T item)
        {
            if (HashSet.Add(item))
            {
                list.Add(item);
                return true;
            }

            return false;
        }
        void ICollection<T>.Add(T item) => Add(item);

        public bool Remove(T key)
        {
            var index = list.IndexOf(key);

            if (index < 0) return false;

            list.RemoveAt(index);

            if (Cached) HashSet.Remove(key);

            return true;
        }

        public void Clear()
        {
            list.Clear();

            if (Cached) HashSet.Clear();
        }

        public void CopyTo(T[] array, int arrayIndex) => (HashSet as ISet<T>).CopyTo(array, arrayIndex);

        public IEnumerator<T> GetEnumerator() => HashSet.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => HashSet.GetEnumerator();

        public UHashSet()
        {
            list = new List<T>();
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            var removals = new HashSet<T>(other);

            list.RemoveAll(removals.Contains);

            if (Cached) HashSet.ExceptWith(other);
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            var collection = new HashSet<T>(other);

            var removals = new HashSet<T>();

            for (int i = 0; i < list.Count; i++)
            {
                if (collection.Contains(list[i]) == false)
                    removals.Add(list[i]);
            }

            list.RemoveAll(removals.Contains);

            if (Cached) HashSet.RemoveWhere(removals.Contains);
        }

        public bool IsProperSubsetOf(IEnumerable<T> other) => HashSet.IsProperSubsetOf(other);
        public bool IsProperSupersetOf(IEnumerable<T> other) => HashSet.IsProperSupersetOf(other);

        public bool IsSubsetOf(IEnumerable<T> other) => HashSet.IsSubsetOf(other);
        public bool IsSupersetOf(IEnumerable<T> other) => HashSet.IsSupersetOf(other);

        public bool Overlaps(IEnumerable<T> other) => HashSet.Overlaps(other);

        public bool SetEquals(IEnumerable<T> other) => HashSet.SetEquals(other);

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            var collections = new HashSet<T>(other);

            var additions = new HashSet<T>();
            var removals = new HashSet<T>();

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (collections.Contains(list[i]))
                    removals.Add(list[i]);
                else
                    additions.Add(list[i]);
            }

            list.RemoveAll(removals.Contains);
            list.AddRange(additions);

            if (Cached)
            {
                HashSet.RemoveWhere(removals.Contains);
                HashSet.UnionWith(additions);
            }
        }

        public void UnionWith(IEnumerable<T> other)
        {
            var additions = new HashSet<T>();

            foreach (var element in other)
            {
                if (HashSet.Contains(element)) continue;

                additions.Add(element);
            }

            list.AddRange(additions);
            HashSet.UnionWith(additions);
        }
    }
}