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
			private static readonly HashSet<Object> hashset;

			internal static HashSet<Object> Load()
			{
				hashset.Clear();

				var array = Get();
				hashset.UnionWith(array);

				hashset.RemoveWhere(IsNull);
				bool IsNull(Object target) => target == null;

				return hashset;
			}

			static Cache()
			{
				hashset = new HashSet<Object>();
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
			
			collection.UnionWith(range);

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

			collection.ExceptWith(range);

			Set(collection);
		}

		#region Disposable
		private static bool LeaseLock;
		
		/// <summary>
		/// Retrieve a Disposable handle and hashset object that will operate on the PreloadedAssets when disposed
		/// </summary>
		/// <param name="hashSet"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public static Handle Lease(out HashSet<Object> hashSet)
		{
			if (LeaseLock)
				throw new InvalidOperationException("Cannot Lease Multiple PreloadedAssets Handles");
			
			LeaseLock = true;
			
			hashSet = Cache.Load();

			return new Handle(hashSet);
		}
		
		internal static void Return(Handle handle)
		{
			LeaseLock = false;
			
			Set(handle.hashset);
		}

		public struct Handle : IDisposable
		{
			internal HashSet<Object> hashset;

			public void Dispose() => Return(this);

			public Handle(HashSet<Object> hashset)
			{
				this.hashset = hashset;
			}
		}
		#endregion
	}
}
#endif