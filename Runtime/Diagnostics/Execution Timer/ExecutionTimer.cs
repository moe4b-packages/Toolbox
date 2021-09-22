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

using System.Diagnostics;

using Debug = UnityEngine.Debug;

namespace MB
{
	/// <summary>
	/// A stopwatch for code, use it to measure code execution time for the sake of optimizing
	/// </summary>
	public class ExecutionTimer : IDisposable
	{
		string name;
		Stopwatch watch;

		public void Dispose()
		{
			watch.Stop();

			var time = watch.Elapsed.TotalMilliseconds;

			LogMethod($"{name} Execution Time: {time}ms");
		}

		public ExecutionTimer() : this(string.Empty) { }
		public ExecutionTimer(string name)
		{
			this.name = name;
			watch = Stopwatch.StartNew();
		}

		public static ExecutionTimer operator +(ExecutionTimer timer, string name)
		{
			timer.name += name;

			return timer;
		}

		//Static Utility
		public delegate void LogDelegate(object target);
		public static LogDelegate LogMethod { get; set; } = Debug.Log;

		public static ExecutionTimer New => Create("");
		public static ExecutionTimer Create(string name) => new ExecutionTimer(name);

		static ExecutionTimer instance;

		public static void Measure(Action callback, string title = null, int iterations = 1)
		{
			if (title == null) title = callback.Method.Name;

			using (new ExecutionTimer(title))
			{
				for (int i = 0; i < iterations; i++)
					callback();
			}
		}

		public static void Start(string name)
        {
			if (instance != null)
			{
				Debug.LogError($"Execution Timer Instance Already Running");
				return;
			}

			instance = new ExecutionTimer(name);
        }

		public static void Stop()
        {
			instance.Dispose();
			instance = null;
        }
	}
}