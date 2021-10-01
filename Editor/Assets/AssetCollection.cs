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
	/// Collection of all assets in project, used to query for asset types
	/// </summary>
	public static class AssetCollection
	{
		public static List<Object> List { get; private set; }

		static bool Ready => List != null;

		static readonly HashSet<string> Inclusions = new HashSet<string>()
		{
			".asset",
			".cs",
			".asmdef"
		};

		public static event Action OnRefresh;
		public static void Refresh()
		{
			var paths = AssetDatabase.GetAllAssetPaths();

			List = new List<Object>(paths.Length);

			for (int i = 0; i < paths.Length; i++)
			{
				var extension = Path.GetExtension(paths[i]).ToLower();

				if (Inclusions.Contains(extension) == false) continue;

				var asset = AssetDatabase.LoadAssetAtPath<Object>(paths[i]);

				List.Add(asset);
			}

			OnRefresh?.Invoke();
		}

        #region Query
        public static List<T> Query<T>()
		{
			var list = new List<T>();

			for (int i = 0; i < List.Count; i++)
				if (List[i] is T target)
					list.Add(target);

			return list;
		}
		public static List<T> Query<T>(Predicate<T> predicate)
		{
			var list = new List<T>();

			for (int i = 0; i < List.Count; i++)
				if (List[i] is T target && predicate(target))
					list.Add(target);

			return list;
		}

		public static List<Object> Query(Predicate<Object> predicate)
        {
			var list = new List<Object>();

			for (int i = 0; i < List.Count; i++)
				if (predicate(List[i]))
					list.Add(List[i]);

			return list;
        }
        #endregion

        #region Find
        public static T Find<T>()
		{
			for (int i = 0; i < List.Count; i++)
				if (List[i] is T target)
					return target;

			return default;
		}
		public static T Find<T>(Predicate<T> predicate)
		{
			for (int i = 0; i < List.Count; i++)
				if (List[i] is T target && predicate(target))
					return target;

			return default;
		}

		public static Object Find(Predicate<Object> predicate)
		{
			for (int i = 0; i < List.Count; i++)
				if (predicate(List[i]))
					return List[i];

			return default;
		}
		#endregion

		static AssetCollection()
		{
			Refresh();
		}

		class FileImporter : AssetPostprocessor
		{
			public static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moveDestination, string[] moveOrigin)
			{
				//Debug.Log("Imported Assets: " + imported.ToCollectionString());
				//Debug.Log("Deleted: " + deleted.ToCollectionString());
				//Debug.Log("Moved Destination: " + moveDestination.ToCollectionString());
				//Debug.Log("Moved Origin: " + moveOrigin.ToCollectionString());

				if (Ready) Refresh();
			}
		}
	}
}
#endif