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
	public abstract class Operation : MonoBehaviour, IOperation
	{
		public const string Path = Toolbox.Paths.Box + "Operations/";
		
		public abstract void Execute();
	}

	public interface IOperation
	{
		void Execute();
	}
}