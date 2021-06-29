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
	public static class MainThreadDispatcher
	{
		[RuntimeInitializeOnLoadMethod]
		static void OnLoad()
        {
			MUtility.RegisterPlayerLoop<Update>(Update);
		}

		public static ConcurrentQueue<Action> Queue { get; private set; }

		public const int ASyncPollRate = 1;

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

		static void Update()
		{
			while (Queue.TryDequeue(out var action))
				action();
		}

		static MainThreadDispatcher()
        {
			Queue = new ConcurrentQueue<Action>();
		}
	}
}