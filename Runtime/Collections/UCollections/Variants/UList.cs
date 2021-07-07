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
    [Serializable]
	public abstract class UList : UCollection
    {
#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(UList), true)]
        public class Drawer : BaseDrawer
        {
            protected override SerializedProperty GetList() => Property.FindPropertyRelative("collection");

            public override void Draw(Rect rect)
            {
                EditorGUIUtility.labelWidth = 120f;

                base.Draw(rect);
            }
        }
#endif
    }

    [Serializable]
    public class UList<T> : UList, IList<T>, IList
    {
        [SerializeField]
        List<T> collection = default;
        public List<T> Collection => collection;

        public IList ICollection => collection;

		public override int Count => collection.Count;

        public bool IsReadOnly => ICollection.IsReadOnly;
        public bool IsFixedSize => ICollection.IsFixedSize;
        public bool IsSynchronized => ICollection.IsSynchronized;
        public object SyncRoot => ICollection.SyncRoot;

        public T this[int index]
        {
			get => collection[index];
			set => collection[index] = value;
        }
        object IList.this[int index]
        {
            get => ICollection[index];
            set => ICollection[index] = value;
        }

        public IEnumerator<T> GetEnumerator() => Collection.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Collection.GetEnumerator();

        public int IndexOf(T item) => collection.IndexOf(item);
        public int IndexOf(object value) => ICollection.IndexOf(value);

        public void Insert(int index, T item) => collection.Insert(index, item);
        public void Insert(int index, object value) => ICollection.Insert(index, value);

        public void RemoveAt(int index) => collection.RemoveAt(index);

        public void Add(T item) => Add(item);
        public int Add(object value) => ICollection.Add(value);

        public void Clear() => collection.Clear();

        public bool Contains(T item) => collection.Contains(item);
        public bool Contains(object value) => ICollection.Contains(value);

        public void CopyTo(T[] array, int arrayIndex) => collection.CopyTo(array, arrayIndex);
        public void CopyTo(Array array, int index) => ICollection.CopyTo(array, index);

        public bool Remove(T item) => collection.Remove(item);
        public void Remove(object value) => ICollection.Remove(value);
    }
}