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
	public class EnumFlagExample : MonoBehaviour
	{
		[EnumFlag]
		[SerializeField]
		EnumChoicesExample choice = EnumChoicesExample.First;

		[EnumFlag]
		[SerializeField]
		EnumChoicesExample[] choices = new EnumChoicesExample[]
		{
			EnumChoicesExample.None,
			EnumChoicesExample.First | EnumChoicesExample.Second,
			EnumChoicesExample.Second | EnumChoicesExample.Third,
			EnumChoicesExample.Third | EnumChoicesExample.Fourth,
			EnumChoicesExample.Fourth | EnumChoicesExample.Fifth,
			EnumChoicesExample.Everything,
		};

        void Start()
        {
			Debug.Log($"Choice: ({choice})");
			Debug.Log($"Choices: ({choices})");
		}
    }

	[Flags]
	public enum EnumChoicesExample
	{
		None = 0,

		Everything = ~0,

		First = 1 << 0,
		Second = 1 << 1,
		Third = 1 << 2,
		Fourth = 1 << 3,
		Fifth = 1 << 4,
	}
}