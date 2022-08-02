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
	/// A single purpose operation that can be executed, base class for operation variants
	/// </summary>
	[AddComponentMenu(Path + "Operation")]
	public class Operation : MonoBehaviour, PreAwake.IInterface
	{
		public const string Path = Toolbox.Paths.Box + "Operations/";

		[ReadOnly]
		[SerializeField]
		Process[] processes;
		public abstract class Process : MonoBehaviour
		{
			public const string Path = Operation.Path + "Process/";

			protected virtual void Reset()
			{
#if UNITY_EDITOR
				if (GetComponentInParent<Operation>() == null)
				{
					var operation = gameObject.AddComponent<Operation>();
					ComponentUtility.MoveComponentUp(operation);
				}
#endif
			}

			/// <summary>
			/// Returns yieldable object when the operation wishes to execute over a period of time
			/// </summary>
			/// <returns>Yieldable Object</returns>
			public abstract object Execute();
		}

		public virtual void PreAwake()
		{
			processes = Query(gameObject);
		}

		public virtual MRoutine.Handle Execute()
		{
			return MRoutine.Create(Procedure()).Start();

			IEnumerator Procedure()
			{
				for (int i = 0; i < processes.Length; i++)
				{
					var result = processes[i].Execute();
					if (result == default) continue;
					yield return result;
				}
			}
		}

		//Static Utility

		public static Process[] Query(GameObject gameObject)
		{
			return gameObject.GetComponentsInChildren<Process>(true);
		}
	}
}