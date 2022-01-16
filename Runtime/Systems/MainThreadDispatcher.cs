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

using UnityEngine.PlayerLoop;

using System.Threading;
using System.Threading.Tasks;

using System.Collections.Concurrent;

namespace MB
{
	/// <summary>
	/// Thread dispatcher to be used to execute actions on the main thread
	/// </summary>
	public static class MainThreadDispatcher
	{
		public static ConcurrentQueue<Action> Queue { get; private set; }

		public static int ASyncPollRate { get; set; } = 1;

		public static void Initialize()
		{
			MUtility.RegisterPlayerLoop<Update>(Update);
		}

		static void Update()
		{
			while (Queue.TryDequeue(out var action))
				action();
		}

		public static void Execute(Action method)
        {
			Queue.Enqueue(method);
		}

		public static async Task ExecuteAsync(Action method)
		{
			bool finished = false;

			Execute(Surrogate);
			void Surrogate()
			{
				method();
				finished = true;
			}

			while (finished == false)
				await Task.Delay(ASyncPollRate);
		}

		public static async Task<T> ExecuteAsync<T>(Func<T> method)
		{
			bool finished = false;
			T result = default;

			Execute(Surrogate);
			void Surrogate()
			{
				result = method();
				finished = true;
			}

			while (finished == false)
				await Task.Delay(ASyncPollRate);

			return result;
		}

		static MainThreadDispatcher()
        {
			Queue = new ConcurrentQueue<Action>();
		}
	}
}