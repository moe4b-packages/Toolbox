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
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditorInternal;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace MB
{
#if UNITY_EDITOR
	/// <summary>
	/// A preprocessor for ScriptableObjects,
	/// implement the interface on ScriptableObjects and recieve the callback before build
	/// </summary>
	public class ScriptableObjectBuildPreProcessor : IPreprocessBuildWithReport
	{
		public int callbackOrder => 0;

		public void OnPreprocessBuild(BuildReport report)
		{
			var list = AssetQuery<ScriptableObject>.FindAll<IScriptableObjectBuildPreProcess>();

			for (int i = 0; i < list.Count; i++)
				list[i].PreProcessBuild();
		}
	}
#endif

	public interface IScriptableObjectBuildPreProcess
	{
#if UNITY_EDITOR
		void PreProcessBuild();
#endif
	}
}