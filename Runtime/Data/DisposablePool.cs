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

using Collections = System.Collections.Generic;
using Text = System.Text;

namespace MB
{
	/// <summary>
	/// A non-thread safe pool that exposes a disposable apporach to pooling
	/// </summary>
	public static class DisposablePool
	{
		public static class Object<T>
			where T : class, new()
		{
			static T root;
			static Queue<T> queue;

			public static CreateDelegate<T> CreateMethod;
			public static Handle<T> Lease(out T item)
			{
				if (root != null)
				{
					item = root;
					root = null;
				}
				else if (queue.Count > 0)
				{
					item = queue.Dequeue();
				}
				else
				{
					item = CreateMethod();
				}

				return new Handle<T>(item);
			}

			public static ClearDelegate<T> ClearMethod;
			internal static void Return(T item)
			{
				ClearMethod(item);

				if (root == null)
					root = item;
				else
					queue.Enqueue(item);
			}

			static Object()
			{
				queue = new Queue<T>();

				CreateMethod = Create;
				static T Create() => new T();

				ClearMethod = Clear;
				static void Clear(T item)
                {
					Debug.LogWarning($"No Clear Method Implemented for Disposable Pool of '{typeof(T)}', You Might Want to Set That");
				}
			}
		}

		public delegate T CreateDelegate<T>() where T : class, new();
		public delegate void ClearDelegate<T>(T item) where T : class, new();

		public readonly struct Handle<T> : IDisposable
			where T : class, new()
		{
			readonly T item;
			public readonly T Item => item;

			public void Dispose()
			{
				Object<T>.Return(item);
			}

			public Handle(T item)
			{
				this.item = item;
			}
		}

		public static class List<T>
		{
			public static CreateDelegate<Collections.List<T>> CreateMethod
			{
				get => Object<Collections.List<T>>.CreateMethod;
				set => Object<Collections.List<T>>.CreateMethod = value;
			}
			public static ClearDelegate<Collections.List<T>> ClearMethod
			{
				get => Object<Collections.List<T>>.ClearMethod;
				set => Object<Collections.List<T>>.ClearMethod = value;
			}

			public static Handle<Collections.List<T>> Lease(out Collections.List<T> collection) => Object<Collections.List<T>>.Lease(out collection);

			static List()
			{
				ClearMethod = Clear;
				static void Clear(Collections.List<T> collection) => collection.Clear();
			}
		}

		public static class HashSet<T>
		{
			public static CreateDelegate<Collections.HashSet<T>> CreateMethod
			{
				get => Object<Collections.HashSet<T>>.CreateMethod;
				set => Object<Collections.HashSet<T>>.CreateMethod = value;
			}
			public static ClearDelegate<Collections.HashSet<T>> ClearMethod
			{
				get => Object<Collections.HashSet<T>>.ClearMethod;
				set => Object<Collections.HashSet<T>>.ClearMethod = value;
			}

			public static Handle<Collections.HashSet<T>> Lease(out Collections.HashSet<T> collection) => Object<Collections.HashSet<T>>.Lease(out collection);

			static HashSet()
			{
				ClearMethod = Clear;
				static void Clear(Collections.HashSet<T> collection) => collection.Clear();
			}
		}

		public static class Dictionary<TKey, TValue>
		{
			public static CreateDelegate<Collections.Dictionary<TKey, TValue>> CreateMethod
			{
				get => Object<Collections.Dictionary<TKey, TValue>>.CreateMethod;
				set => Object<Collections.Dictionary<TKey, TValue>>.CreateMethod = value;
			}
			public static ClearDelegate<Collections.Dictionary<TKey, TValue>> ClearMethod
			{
				get => Object<Collections.Dictionary<TKey, TValue>>.ClearMethod;
				set => Object<Collections.Dictionary<TKey, TValue>>.ClearMethod = value;
			}

			public static Handle<Collections.Dictionary<TKey, TValue>> Lease(out Collections.Dictionary<TKey, TValue> collection) => Object<Collections.Dictionary<TKey, TValue>>.Lease(out collection);

			static Dictionary()
			{
				ClearMethod = Clear;
				static void Clear(Collections.Dictionary<TKey, TValue> collection) => collection.Clear();
			}
		}

		public static class StringBuilder
        {
			public static CreateDelegate<Text.StringBuilder> CreateMethod
			{
				get => Object<Text.StringBuilder>.CreateMethod;
				set => Object<Text.StringBuilder>.CreateMethod = value;
			}
			public static ClearDelegate<Text.StringBuilder> ClearMethod
			{
				get => Object<Text.StringBuilder>.ClearMethod;
				set => Object<Text.StringBuilder>.ClearMethod = value;
			}

			public static Handle<Text.StringBuilder> Lease(out Text.StringBuilder collection) => Object<Text.StringBuilder>.Lease(out collection);

			static StringBuilder()
			{
				ClearMethod = Clear;
				static void Clear(Text.StringBuilder collection) => collection.Clear();
			}
		}
	}
}