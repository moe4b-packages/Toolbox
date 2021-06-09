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
            protected override SerializedProperty GetList() => Property.FindPropertyRelative("list");

            public SerializedProperty GetKey(int index) => List.GetArrayElementAtIndex(index).FindPropertyRelative("key");
            public SerializedProperty GetValue(int index) => List.GetArrayElementAtIndex(index).FindPropertyRelative("value");

            HashSet<int> duplicates;
            HashSet<int> nullables;

            public const float NestedElementSpacing = 2f;

            public const float KeyValuePadding = 10f;

            public const float KeyInfoContextWidth = 20f;

            static GUIContent ConflictGUIContent = GetIconContent("console.warnicon.sml", "Conflicting Key, Data Might be Lost");
            static GUIContent NullGUIContent = GetIconContent("console.erroricon.sml", "Null Key, Will be Ignored");

            protected override void Init()
            {
                base.Init();

                duplicates = new HashSet<int>();
                nullables = new HashSet<int>();
                UpdateState();

                UI.OnChangeElement += ChangeElements;
            }

            #region Height
            protected override float GetElementHeight(int index)
            {
                var key = GetKey(index);
                var value = GetValue(index);

                var kHeight = GetChildrenSingleHeight(key, NestedElementSpacing);
                var vHeight = GetChildrenSingleHeight(value, NestedElementSpacing);

                var max = Math.Max(kHeight, vHeight);
                max = Math.Max(max, SingleLineHeight);

                return max + ElementHeightPadding;
            }
            #endregion

            #region Draw
            public override void Draw(Rect rect)
            {
                EditorGUI.BeginChangeCheck();

                base.Draw(rect);

                if (EditorGUI.EndChangeCheck())
                    UpdateState();
            }

            #region Draw Element
            protected override void DrawElement(Rect rect, int index)
            {
                rect.height -= ElementHeightPadding;
                rect.y += ElementHeightPadding / 2;

                var key = GetKey(index);
                var value = GetValue(index);

                if (nullables.Contains(index))
                {
                    var area = new Rect(rect.x + 2.5f, rect.y, KeyInfoContextWidth, rect.height);

                    EditorGUI.LabelField(area, NullGUIContent);

                    rect.width -= KeyInfoContextWidth;
                    rect.x += KeyInfoContextWidth;
                }

                if (duplicates.Contains(index))
                {
                    var area = new Rect(rect.x + 2.5f, rect.y, KeyInfoContextWidth, rect.height);

                    EditorGUI.LabelField(area, ConflictGUIContent);

                    rect.width -= KeyInfoContextWidth;
                    rect.x += KeyInfoContextWidth;
                }

                var areas = Split(rect, 40, 60);

                DrawKey(areas[0], key, index);
                DrawValue(areas[1], value, index);
            }

            void DrawKey(Rect rect, SerializedProperty property, int index)
            {
                EditorGUIUtility.labelWidth = 60;

                rect.x += KeyValuePadding / 2f;
                rect.width -= KeyValuePadding;

                DrawField(rect, property);
            }

            void DrawValue(Rect rect, SerializedProperty property, int index)
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
            #endregion

            void ChangeElements() => UpdateState();

            void UpdateState()
            {
                duplicates.Clear();
                nullables.Clear();

                var elements = new SerializedProperty[List.arraySize];

                for (int i = 0; i < elements.Length; i++)
                    elements[i] = GetKey(i);

                for (int x = 0; x < elements.Length; x++)
                {
                    if (elements[x].propertyType == SerializedPropertyType.ObjectReference && elements[x].objectReferenceValue == null)
                        nullables.Add(x);

                    if (duplicates.Contains(x) || nullables.Contains(x)) continue;

                    for (int y = 0; y < elements.Length; y++)
                    {
                        if (x == y) continue;

                        if (SerializedProperty.DataEquals(elements[x], elements[y]))
                        {
                            duplicates.Add(x);
                            duplicates.Add(y);
                        }
                    }
                }
            }
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