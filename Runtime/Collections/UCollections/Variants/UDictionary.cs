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
    public abstract class UDictionary : UCollection
    {
#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(UDictionary), true)]
        public class Drawer : BaseDrawer
        {
            protected override SerializedProperty FindListProperty(SerializedProperty property)
            {
                return property.FindPropertyRelative("list");
            }

            public SerializedProperty GetKey(SerializedProperty list, int index)
            {
                return list.GetArrayElementAtIndex(index).FindPropertyRelative("key");
            }
            public SerializedProperty GetValue(SerializedProperty list, int index)
            {
                return list.GetArrayElementAtIndex(index).FindPropertyRelative("value");
            }

            public const float NestedElementSpacing = 2f;

            public const float KeyValuePadding = 10f;

            #region Height
            protected override float GetElementHeight(ImprovedReorderableList list, int index)
            {
                var key = GetKey(list.Property, index);
                var value = GetValue(list.Property, index);

                var kHeight = GetChildrenSingleHeight(key, NestedElementSpacing);
                var vHeight = GetChildrenSingleHeight(value, NestedElementSpacing);

                var max = Math.Max(kHeight, vHeight);
                max = Math.Max(max, SingleLineHeight);

                return max + ElementHeightPadding;
            }
            #endregion

            #region Draw
            protected override void DrawElement(ImprovedReorderableList list, Rect rect, int index)
            {
                rect.height -= ElementHeightPadding;
                rect.y += ElementHeightPadding / 2;

                var areas = Split(rect, 40, 60);

                var key = GetKey(list.Property, index);
                var value = GetValue(list.Property, index);

                DrawKey(areas[0], key);
                DrawValue(areas[1], value);
            }

            void DrawKey(Rect rect, SerializedProperty property)
            {
                EditorGUIUtility.labelWidth = 60;

                rect.x += KeyValuePadding / 2f;
                rect.width -= KeyValuePadding;

                DrawField(rect, property);
            }

            void DrawValue(Rect rect, SerializedProperty property)
            {
                EditorGUIUtility.labelWidth = 80;

                rect.x += KeyValuePadding / 2f;
                rect.width -= KeyValuePadding;

                DrawField(rect, property);
            }

            protected override void DrawField(Rect rect, SerializedProperty property)
            {
                rect.height = SingleLineHeight;

                if (IsInline(property))
                {
                    EditorGUI.PropertyField(rect, property, GUIContent.none);
                }
                else
                {
                    foreach (var child in IterateChildren(property))
                    {
                        EditorGUI.PropertyField(rect, child, false);

                        rect.y += SingleLineHeight + NestedElementSpacing;
                    }
                }
            }
            #endregion
        }
#endif
    }

    [Serializable]
    public class UDictionary<TKey, TValue> : UDictionary, IDictionary<TKey, TValue>
    {
        [SerializeField]
        List<KeyValuePair> list = default;
        public List<KeyValuePair> List => list;

        [Serializable]
        public struct KeyValuePair
        {
            [SerializeField]
            TKey key;
            public TKey Key => key;

            [SerializeField]
            TValue value;
            public TValue Value => value;

            public KeyValuePair(TKey key, TValue value)
            {
                this.key = key;
                this.value = value;
            }
        }

        public ICollection<TKey> Keys => Dictionary.Keys;
        public ICollection<TValue> Values => Dictionary.Values;

        public override int Count => list.Count;

        public bool IsReadOnly => false;

        public int IndexOf(TKey key)
        {
            return list.FindIndex(Predicate);

            bool Predicate(KeyValuePair pair) => Equals(pair.Key, key);
        }

        Dictionary<TKey, TValue> cache;
        public bool Cached => cache != null;
        public Dictionary<TKey, TValue> Dictionary
        {
            get
            {
                if (cache == null)
                {
                    cache = new Dictionary<TKey, TValue>();

                    for (int i = 0; i < List.Count; i++)
                        cache[list[i].Key] = list[i].Value;
                }

                return cache;
            }
        }

        public TValue this[TKey key]
        {
            get => Dictionary[key];
            set
            {
                var index = IndexOf(key);

                if (index < 0)
                {
                    Add(key, value);
                }
                else
                {
                    list[index] = new KeyValuePair(key, value);
                    if (Cached) Dictionary[key] = value;
                }
            }
        }

        public bool TryGetValue(TKey key, out TValue value) => Dictionary.TryGetValue(key, out value);

        public bool ContainsKey(TKey key) => Dictionary.ContainsKey(key);
        public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);

        public void Add(TKey key, TValue value)
        {
            if (ContainsKey(key)) throw new ArgumentException($"Key {key} Already Added With Collection");

            var pair = new KeyValuePair(key, value);
            list.Add(pair);

            if (Cached) Dictionary.Add(key, value);
        }
        public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

        public void AddAll(IDictionary<TKey, TValue> collection)
        {
            foreach (var pair in collection)
                this[pair.Key] = pair.Value;
        }

        public bool Remove(TKey key)
        {
            var index = IndexOf(key);

            if (index < 0) return false;

            list.RemoveAt(index);

            if (Cached) Dictionary.Remove(key);

            return true;
        }
        public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

        public void Clear()
        {
            list.Clear();

            if (Cached) Dictionary.Clear();
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => (Dictionary as IDictionary).CopyTo(array, arrayIndex);

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => Dictionary.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Dictionary.GetEnumerator();

        public UDictionary()
        {
            list = new List<KeyValuePair>();
        }
    }
}