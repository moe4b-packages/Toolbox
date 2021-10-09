#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using Object = UnityEngine.Object;

namespace MB
{
	public static class PreloadedAssets
	{
		private static Object[] Array
		{
			get => PlayerSettings.GetPreloadedAssets();
			set => PlayerSettings.SetPreloadedAssets(value);
		}

		internal static class Cache
		{
			private static readonly HashSet<Object> hashset;

			internal static HashSet<Object> Load()
			{
				hashset.Clear();
				hashset.UnionWith(Array);

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
			var set = Cache.Load();
			
			set.Add(item);
			
			Array = set.ToArray();
		}
		public static void Add(IEnumerable<Object> range)
		{
			var set = Cache.Load();
			
			set.UnionWith(range);
			
			Array = set.ToArray();
		}

		public static void Remove(Object item)
		{
			var set = Cache.Load();

			set.Remove(item);

			Array = set.ToArray();
		}
		public static void Remove(IEnumerable<Object> range)
		{
			var set = Cache.Load();

			set.ExceptWith(range);

			Array = set.ToArray();
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

			Array = handle.hashset.ToArray();
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