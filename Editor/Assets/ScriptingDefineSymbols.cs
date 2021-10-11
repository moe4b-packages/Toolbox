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
	/// Helper class for modifying Scripting Define Symbols easily
	/// </summary>
	public static class ScriptingDefineSymbols
	{
		public static string[] Get(BuildTargetGroup group)
		{
			PlayerSettings.GetScriptingDefineSymbolsForGroup(group, out var array);

			return array;
		}
		public static void Set(BuildTargetGroup group, IEnumerable<string> collection)
		{
			var array = collection.ToArray();

			PlayerSettings.SetScriptingDefineSymbolsForGroup(group, array);
		}

		internal static class Cache
		{
			private static readonly HashSet<string> hashset;

			internal static HashSet<string> Load(BuildTargetGroup group)
			{
				hashset.Clear();

				var array = Get(group);
				hashset.UnionWith(array);

				return hashset;
			}

			static Cache()
			{
				hashset = new HashSet<string>();
			}
		}

		public static void Add(BuildTargetGroup group, string item)
		{
			var set = Cache.Load(group);
			
			set.Add(item);
			
			Set(group, set);
		}
		public static void Add(BuildTargetGroup group, IEnumerable<string> range)
		{
			var set = Cache.Load(group);
			
			set.UnionWith(range);
			
			Set(group, set);
		}

		public static void Remove(BuildTargetGroup group, string item)
		{
			var set = Cache.Load(group);

			set.Remove(item);

			Set(group, set);
		}
		public static void Remove(BuildTargetGroup group, IEnumerable<string> range)
		{
			var set = Cache.Load(group);

			set.ExceptWith(range);

			Set(group, set);
		}

		#region Disposable
		private static bool LeaseLock;
		
		/// <summary>
		/// Retrieve a Disposable handle and hashset object that will operate on the Scripting Symbols after disposal
		/// </summary>
		/// <param name="group"></param>
		/// <param name="hashSet"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public static Handle Lease(BuildTargetGroup group, out HashSet<string> hashSet)
		{
			if (LeaseLock)
				throw new InvalidOperationException("Cannot Lease Multiple PreloadedAssets Handles");
			
			LeaseLock = true;
			
			hashSet = Cache.Load(group);

			return new Handle(group, hashSet);
		}
		
		internal static void Return(Handle handle)
		{
			LeaseLock = false;
			
			Set(handle.group, handle.hashset);
		}

		public readonly struct Handle : IDisposable
		{
			internal readonly BuildTargetGroup group;
			internal readonly HashSet<string> hashset;

			public void Dispose() => Return(this);

			public Handle(BuildTargetGroup group, HashSet<string> hashset)
			{
				this.group = group;
				this.hashset = hashset;
			}
		}
		#endregion
	}
}
#endif