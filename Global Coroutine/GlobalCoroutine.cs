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
	public class GlobalCoroutine : MonoBehaviour
	{
		public static GlobalCoroutine Instance { get; protected set; }

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
		static void OnLoad() => Configure();

		/// <summary>
		/// Manually Configure, To Ensure that this Class is Initiated Before You Use It
		/// </summary>
		public static void Configure()
		{
			if (Instance) return;

			var gameObject = new GameObject("Global Coroutine");

			Instance = gameObject.AddComponent<GlobalCoroutine>();
			DontDestroyOnLoad(Instance);
		}

		public static Coroutine Start(Func<IEnumerator> function) => Start(function());
		public static Coroutine Start(IEnumerator ienumerator) => Instance.StartCoroutine(ienumerator);

		public static void Stop(Coroutine coroutine) => Instance.StopCoroutine(coroutine);

		public static void StopAll() => Instance.StopAllCoroutines();
	}
}