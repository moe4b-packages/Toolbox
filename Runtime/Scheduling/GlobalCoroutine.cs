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

using UnityEngine.Scripting;

namespace MB
{
	[AddComponentMenu("")]
	/// <summary>
	/// A globally accessible coroutine manager, can be used to start and stop coroutines
	/// </summary>
	public class GlobalCoroutine : MonoBehaviour
	{
		public static GlobalCoroutine Instance { get; protected set; }

		public static bool Ready => Instance != null;

		/// <summary>
		/// Manually Configure, To Ensure that this Class is Initiated Before You Use It
		/// </summary>
		public static void Configure()
		{
			if (Ready) return;

			var gameObject = new GameObject("Global Coroutine");
			DontDestroyOnLoad(gameObject);

			Instance = gameObject.AddComponent<GlobalCoroutine>();
		}

		public static Coroutine Start(Func<IEnumerator> function) => Start(function());
		public static Coroutine Start(IEnumerator ienumerator)
        {
			if (Instance == null) throw new Exception("Global Coroutine Not Configured Yet");

			return Instance.StartCoroutine(ienumerator);
		}

		public static void Stop(Coroutine coroutine) => Instance.StopCoroutine(coroutine);

		public static void StopAll() => Instance.StopAllCoroutines();

		static GlobalCoroutine()
		{
#if UNITY_EDITOR
			if (Application.isPlaying == false)
				throw new Exception("Global Coroutines Cannot be Used in edit-time, Only at play/run-Time");
#endif

			if (Ready == false) Configure();
		}
	}
}