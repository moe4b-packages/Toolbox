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

using System.Reflection;

namespace MB
{
    [Serializable]
    public class UHashSet<T> : ISet<T>, IReadOnlyCollection<T>, ISerializationCallbackReceiver
    {
        public const int MiniumumCapacity = 1;

        [SerializeField]
        List<Entry> list;
        [Serializable]
        public struct Entry : IEquatable<Entry>
        {
            [field: SerializeField]
            public T Value { get; private set; }

            [field: SerializeField]
            public bool IsValid { get; private set; }

            public Entry SetValid(bool value)
            {
                IsValid = value;
                return this;
            }

            public void Deconstruct(out T value)
            {
                value = this.Value;
            }

            public override bool Equals(object obj)
            {
                if (obj is Entry entry)
                    return Equals(entry);

                return false;
            }
            public bool Equals(Entry entry)
            {
                if (EqualityComparer<T>.Default.Equals(Value, entry.Value) == false)
                    return false;

                return true;
            }

            public override int GetHashCode() => (Value?.GetHashCode()).GetValueOrDefault(0);

            public Entry(T value)
            {
                this.Value = value;
                IsValid = true;
            }
        }

        [field: NonSerialized]
        public HashSet<T> Backing { get; }

        #region Interface Implementation
        public int Count => ((ICollection<T>)Backing).Count;

        public bool IsReadOnly => ((ICollection<T>)Backing).IsReadOnly;

        public bool Add(T item)
        {
            return ((ISet<T>)Backing).Add(item);
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            ((ISet<T>)Backing).ExceptWith(other);
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            ((ISet<T>)Backing).IntersectWith(other);
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return ((ISet<T>)Backing).IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return ((ISet<T>)Backing).IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return ((ISet<T>)Backing).IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return ((ISet<T>)Backing).IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            return ((ISet<T>)Backing).Overlaps(other);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            return ((ISet<T>)Backing).SetEquals(other);
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            ((ISet<T>)Backing).SymmetricExceptWith(other);
        }

        public void UnionWith(IEnumerable<T> other)
        {
            ((ISet<T>)Backing).UnionWith(other);
        }

        void ICollection<T>.Add(T item)
        {
            ((ICollection<T>)Backing).Add(item);
        }

        public void Clear()
        {
            ((ICollection<T>)Backing).Clear();
        }

        public bool Contains(T item)
        {
            return ((ICollection<T>)Backing).Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            ((ICollection<T>)Backing).CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return ((ICollection<T>)Backing).Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)Backing).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Backing).GetEnumerator();
        }
        #endregion

        #region Serialization
        object SerializationLock = new object();

        public void OnBeforeSerialize()
        {
            lock (SerializationLock)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    list[i] = list[i].SetValid(Pool.Values.Add(list[i].Value));
                    Pool.Entries.Add(list[i]);
                }

                foreach (var item in Backing)
                {
                    var entry = new Entry(item);

                    if (Pool.Entries.Contains(entry))
                        continue;

                    list.Add(entry);
                }

                Pool.Clear();
            }
        }

        public void OnAfterDeserialize()
        {
            Backing.Clear();

            foreach (var entry in list)
            {
                if (entry.Value == null)
                    continue;

                Backing.Add(entry.Value);
            }
        }
        #endregion

        public UHashSet()
        {
            list = new List<Entry>();
            Backing = new HashSet<T>();
        }
        public UHashSet(int capacity)
        {
            list = new List<Entry>(capacity);
            Backing = new HashSet<T>(capacity);
        }

        public static class Pool
        {
            public static HashSet<Entry> Entries;
            public static HashSet<T> Values;

            public static void Clear()
            {
                Entries.Clear();
                Values.Clear();
            }

            static Pool()
            {
                Entries = new();
                Values = new();
            }
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(UHashSet<>.Entry), true)]
    public class UHashSetEntryDrawer : UCollectionEntryDrawer
    {
        public static void Initiate(SerializedProperty property, out SerializedProperty value)
        {
            value = property.FindPropertyRelative(MUtility.Type.FormatPropertyBackingFieldName("Value"));
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            Initiate(property, out var value);

            return EditorGUI.GetPropertyHeight(value, true) + Padding;
        }

        protected override void DrawContent(Rect rect, SerializedProperty property, GUIContent label, bool isValid)
        {
            Initiate(property, out var value);

            //EditorGUI.PropertyField(rect, value, label, true);
            DrawShortField(rect, value);
        }
    }
#endif
}