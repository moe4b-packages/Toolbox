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
	/// A Utility for quering ScriptableObjects, Editor only script
	/// </summary>
	public static class ScriptableObjectQuery
	{
		public static List<ScriptableObject> Collection { get; private set; }

		public static List<T> FindAll<T>()
		{
			var list = new List<T>();

			for (int i = 0; i < Collection.Count; i++)
			{
				if (Collection[i] is T element)
					list.Add(element);
			}

			return list;
		}

		public static List<ScriptableObject> FindAllWith<T>()
        {
			var list = new List<ScriptableObject>();

			for (int i = 0; i < Collection.Count; i++)
			{
				if (Collection[i] is T)
					list.Add(Collection[i]);
			}

			return list;
		}

		public class FileImporter : AssetPostprocessor
		{
			public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
			{
				if (Collection == null) return;

				Refresh();
			}
		}

		static void Refresh()
		{
			Collection.Clear();

			var guids = AssetDatabase.FindAssets($"t:{nameof(ScriptableObject)}");

			for (int i = 0; i < guids.Length; i++)
			{
				var path = AssetDatabase.GUIDToAssetPath(guids[i]);

				var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

				Collection.Add(asset);
			}
		}

		static ScriptableObjectQuery()
		{
			Collection = new List<ScriptableObject>();

			Refresh();
		}
	}
}
#endif