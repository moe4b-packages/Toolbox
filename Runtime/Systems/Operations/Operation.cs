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
	public class Operation : MonoBehaviour
	{
		public const string Path = Toolbox.Paths.Box + "Operations/";

		public Process[] Processes { get; private set; }

		public abstract class Process : MonoBehaviour
		{
			public const string Path = Operation.Path;

			protected virtual void Reset()
			{
#if UNITY_EDITOR
				if(GetComponentInParent<Operation>() == null)
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

		protected virtual void Awake()
		{
			Processes = Query(gameObject);
		}

		public virtual Coroutine Execute()
        {
			return StartCoroutine(Iterate());
		}
		public virtual IEnumerator Iterate()
        {
			return Procedure();
			IEnumerator Procedure()
			{
				for (int i = 0; i < Processes.Length; i++)
				{
					var result = Processes[i].Execute();
					if (result == null) continue;
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