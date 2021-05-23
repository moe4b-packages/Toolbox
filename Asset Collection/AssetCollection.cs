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
			var pathes = AssetDatabase.GetAllAssetPaths();

			List = new List<Object>(pathes.Length);

			for (int i = 0; i < pathes.Length; i++)
			{
				var extension = Path.GetExtension(pathes[i]).ToLower();

				if (Inclusions.Contains(extension) == false) continue;

				var asset = AssetDatabase.LoadAssetAtPath<Object>(pathes[i]);

				List.Add(asset);
			}

			OnRefresh?.Invoke();
		}

		public static List<T> Query<T>()
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
			public static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moveDestination, string[] moveOrigin)
			{
				Debug.Log("Imported Assets: " + imported.ToCollectionString());
				//Debug.Log("Deleted: " + deleted.ToCollectionString());
				//Debug.Log("Moved Destination: " + moveDestination.ToCollectionString());
				//Debug.Log("Moved Origin: " + moveOrigin.ToCollectionString());

				if (Ready) Refresh();
			}
		}
	}
}