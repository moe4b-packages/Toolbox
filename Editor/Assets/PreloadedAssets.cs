#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using Object = UnityEngine.Object;

namespace MB
{
	/// <summary>
	/// Helper class for easily modifying Unity's Preloaded Assets
	/// </summary>
	public static class PreloadedAssets
	{
		public static Object[] Get() => PlayerSettings.GetPreloadedAssets();
		public static void Set(IEnumerable<Object> collection)
		{
			var array = collection.ToArray();
			PlayerSettings.SetPreloadedAssets(array);
		}
		
		internal static class Cache
		{
			private static readonly List<Object> list;

			internal static List<Object> Load()
			{
				list.Clear();

				var array = Get();
				list.AddRange(array);

				list.RemoveAll(IsNull);
				bool IsNull(Object target) => target == null;

				return list;
			}

			static Cache()
			{
				list = new List<Object>();
			}
		}

		public static void Add(Object item)
		{
			var collection = Cache.Load();
			
			collection.Add(item);

			Set(collection);
		}
		public static void Add(IEnumerable<Object> range)
		{
			var collection = Cache.Load();

			collection.AddRange(range);

			Set(collection);
		}

		public static void Remove(Object item)
		{
			var collection = Cache.Load();

			collection.Remove(item);

			Set(collection);
		}
		public static void Remove(IEnumerable<Object> range)
		{
			var collection = Cache.Load();

			collection.RemoveAll(Contained);
			bool Contained(Object target) => range.Contains(target);

			Set(collection);
		}

		#region Disposable
		private static bool LeaseLock;
		
		/// <summary>
		/// Retrieve a Disposable handle and hashset object that will operate on the PreloadedAssets when disposed
		/// </summary>
		/// <param name="list"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public static Handle Lease(out List<Object> list)
		{
			if (LeaseLock)
				throw new InvalidOperationException("Cannot Lease Multiple PreloadedAssets Handles");
			
			LeaseLock = true;
			
			list = Cache.Load();

			return new Handle(list);
		}
		
		internal static void Return(Handle handle)
		{
			LeaseLock = false;
			
			Set(handle.list);
		}

		public struct Handle : IDisposable
		{
			internal List<Object> list;

			public void Dispose() => Return(this);

			public Handle(List<Object> hashset)
			{
				this.list = hashset;
			}
		}
		#endregion
	}
}
#endif