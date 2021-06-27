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
    public class DualDictionary<TKey1, TKey2, TValue>
    {
        public Dictionary<TKey1, TValue> Dictionary1 { get; protected set; }
        public Dictionary<TKey2, TValue> Dictionary2 { get; protected set; }

        public ICollection<TKey1> Keys1 => Dictionary1.Keys;
        public ICollection<TKey2> Keys2 => Dictionary2.Keys;

        public ICollection<TValue> Values => Dictionary1.Values;

        public int Count => Dictionary1.Count;

        public TValue this[TKey1 key] => Dictionary1[key];
        public TValue this[TKey2 key] => Dictionary2[key];

        public TValue this[TKey1 key1, TKey2 key2]
        {
            set
            {
                Dictionary1[key1] = value;
                Dictionary2[key2] = value;
            }
        }

        public void Add(TKey1 key1, TKey2 key2, TValue value)
        {
            Dictionary1.Add(key1, value);
            Dictionary2.Add(key2, value);
        }

        public bool Contains(TKey1 key) => Dictionary1.ContainsKey(key);
        public bool Contains(TKey2 key) => Dictionary2.ContainsKey(key);

        public bool TryGetValue(TKey1 key, out TValue value) => Dictionary1.TryGetValue(key, out value);
        public bool TryGetValue(TKey2 key, out TValue value) => Dictionary2.TryGetValue(key, out value);

        public bool Remove(TKey1 key1, TKey2 key2)
        {
            var removed = false;

            removed |= Dictionary1.Remove(key1);
            removed |= Dictionary2.Remove(key2);

            return removed;
        }

        public void Clear()
        {
            Dictionary1.Clear();
            Dictionary2.Clear();
        }

        public DualDictionary() : this(0) { }
        public DualDictionary(int capacity)
        {
            Dictionary1 = new Dictionary<TKey1, TValue>(capacity);
            Dictionary2 = new Dictionary<TKey2, TValue>(capacity);
        }
    }
}