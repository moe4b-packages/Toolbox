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
using UnityEngine.Serialization;

namespace MB
{
    [Serializable]
    public class UDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary, IReadOnlyCollection<UDictionary<TKey, TValue>.Entry>, ISerializationCallbackReceiver
    {
        [SerializeField]
        List<Entry> list;
        [Serializable]
        public struct Entry : IEquatable<Entry>
        {
            [field: SerializeField, FormerlySerializedAs("key")]
            public TKey Key { get; private set; }

            [field: SerializeField, FormerlySerializedAs("value")]
            public TValue Value { get; private set; }

            [field: SerializeField]
            internal bool IsValid { get; private set; }

            public Entry SetValid(bool value)
            {
                IsValid = value;
                return this;
            }

            public void Deconstruct(out TKey key, out TValue value)
            {
                key = this.Key;
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
                if (EqualityComparer<TKey>.Default.Equals(Key, entry.Key) == false)
                    return false;

                if (EqualityComparer<TValue>.Default.Equals(Value, entry.Value) == false)
                    return false;

                return true;
            }

            public override int GetHashCode() => HashCode.Combine(Key, Value);

            public Entry(TKey key, TValue value)
            {
                this.Key = key;
                this.Value = value;
                IsValid = true;
            }
        }

        [field: NonSerialized]
        public Dictionary<TKey, TValue> Backing { get; }

        #region Interface Implementation
        public ICollection<TKey> Keys => ((IDictionary<TKey, TValue>)Backing).Keys;

        public ICollection<TValue> Values => ((IDictionary<TKey, TValue>)Backing).Values;

        public int Count => ((ICollection<KeyValuePair<TKey, TValue>>)Backing).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)Backing).IsReadOnly;

        public bool IsFixedSize => ((IDictionary)Backing).IsFixedSize;

        ICollection IDictionary.Keys => ((IDictionary)Backing).Keys;

        ICollection IDictionary.Values => ((IDictionary)Backing).Values;

        public bool IsSynchronized => ((ICollection)Backing).IsSynchronized;

        public object SyncRoot => ((ICollection)Backing).SyncRoot;

        public object this[object key] { get => ((IDictionary)Backing)[key]; set => ((IDictionary)Backing)[key] = value; }
        public TValue this[TKey key] { get => ((IDictionary<TKey, TValue>)Backing)[key]; set => ((IDictionary<TKey, TValue>)Backing)[key] = value; }

        public void Add(TKey key, TValue value)
        {
            ((IDictionary<TKey, TValue>)Backing).Add(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            return ((IDictionary<TKey, TValue>)Backing).ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            return ((IDictionary<TKey, TValue>)Backing).Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return ((IDictionary<TKey, TValue>)Backing).TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)Backing).Add(item);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)Backing).Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)Backing).Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)Backing).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)Backing).Remove(item);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<TKey, TValue>>)Backing).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Backing).GetEnumerator();
        }

        public void Add(object key, object value)
        {
            ((IDictionary)Backing).Add(key, value);
        }

        public bool Contains(object key)
        {
            return ((IDictionary)Backing).Contains(key);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return ((IDictionary)Backing).GetEnumerator();
        }

        public void Remove(object key)
        {
            ((IDictionary)Backing).Remove(key);
        }

        public void CopyTo(Array array, int index)
        {
            ((ICollection)Backing).CopyTo(array, index);
        }

        IEnumerator<Entry> IEnumerable<Entry>.GetEnumerator()
        {
            return ((IEnumerable<Entry>)list).GetEnumerator();
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
                    list[i] = list[i].SetValid(Pool.Keys.Add(list[i].Key));
                    Pool.Entries.Add(list[i]);
                }

                foreach (var (key, value) in Backing)
                {
                    var entry = new Entry(key, value);

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

            foreach (var (key, value) in list)
            {
                if (key == null)
                    continue;

                Backing.TryAdd(key, value);
            }
        }
        #endregion

        public UDictionary()
        {
            list = new List<Entry>();
            Backing = new Dictionary<TKey, TValue>();
        }
        public UDictionary(int capacity)
        {
            list = new List<Entry>(capacity);
            Backing = new Dictionary<TKey, TValue>(capacity);
        }

        public static class Pool
        {
            public static HashSet<Entry> Entries;
            public static HashSet<TKey> Keys;

            public static void Clear()
            {
                Entries.Clear();
                Keys.Clear();
            }

            static Pool()
            {
                Entries = new();
                Keys = new();
            }
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(UDictionary<,>.Entry), true)]
    public class UDictionaryEntryDrawer : UCollectionEntryDrawer
    {
        public const float KeyValueSpacing = 5f;

        public static void Initiate(SerializedProperty property, out SerializedProperty key, out SerializedProperty value)
        {
            key = property.FindPropertyRelative(MUtility.Type.FormatPropertyBackingFieldName("Key"));
            value = property.FindPropertyRelative(MUtility.Type.FormatPropertyBackingFieldName("Value"));
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            Initiate(property, out var key, out var value);

            var keyHeight = EditorGUI.GetPropertyHeight(key, true);
            var valueHeight = EditorGUI.GetPropertyHeight(value, true);

            return Math.Max(keyHeight, valueHeight) + Padding;
        }

        protected override void DrawContent(Rect rect, SerializedProperty property, GUIContent label, bool isValid)
        {
            Initiate(property, out var key, out var value);

            var area = MUtility.GUI.SplitHorizontally(rect, 0, 35f, 65f);

            DrawKey(area[0], key);
            DrawValue(area[1], value);
        }

        public static void DrawKey(Rect rect, SerializedProperty property)
        {
            rect.xMax -= KeyValueSpacing;
            DrawShortField(rect, property);
        }
        public static void DrawValue(Rect rect, SerializedProperty property)
        {
            rect.xMin += KeyValueSpacing;
            DrawShortField(rect, property);
        }
    }
#endif
}