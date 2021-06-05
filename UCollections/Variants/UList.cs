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
    public class UList<T> : UList
    {
		[SerializeField]
		List<T> collection = default;
		public List<T> Collection => collection;

		public override int Count => collection.Count;
        public T this[int index]
        {
			get => collection[index];
			set => collection[index] = value;
        }

		public IEnumerator<T> GetEnumerator() => collection.GetEnumerator();
	}
}