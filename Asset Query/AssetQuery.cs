#if UNITY_EDITOR
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
	/// <summary>
	/// A Utility for quering Assets in Project, Editor only script
	/// </summary>
	public static class AssetQuery<T>
		where T : Object
	{
		public static List<T> Collection { get; private set; }

		static void Refresh()
		{
			Collection = AssetCollection.FindAll<T>();
		}

		public static List<T> ToList() => Collection.ToList();

		public static List<T> FindAll(Predicate<T> predicate)
		{
			var list = new List<T>();

			for (int i = 0; i < Collection.Count; i++)
				if (predicate(Collection[i]))
					list.Add(Collection[i]);

			return list;
		}

		public static List<TTarget> FindAll<TTarget>()
		{
			var list = new List<TTarget>();

			for (int i = 0; i < Collection.Count; i++)
				if (Collection[i] is TTarget element)
					list.Add(element);

			return list;
		}

		static AssetQuery()
		{
			AssetCollection.OnRefresh += Refresh;

			Refresh();
		}
	}

	public static class AssetCollection
	{
		public static List<Object> List { get; private set; }

		static bool Ready => List != null;

		public static event Action OnRefresh;
		public static void Refresh()
		{
			var pathes = AssetDatabase.GetAllAssetPaths();

			List = new List<Object>(pathes.Length);

			for (int i = 0; i < pathes.Length; i++)
			{
				var asset = AssetDatabase.LoadAssetAtPath<Object>(pathes[i]);
				List.Add(asset);
			}

			OnRefresh?.Invoke();
		}

		public static List<T> FindAll<T>()
		{
			var list = new List<T>();

			for (int i = 0; i < List.Count; i++)
				if (List[i] is T target)
					list.Add(target);

			return list;
		}

		static AssetCollection()
		{
			Refresh();
		}

		class FileImporter : AssetPostprocessor
		{
			public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
			{
				if (Ready) Refresh();
			}
		}
	}
}
#endif