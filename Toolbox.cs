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

[assembly: AssemblySymbolDefine("MB_TOOLBOX")]

namespace MB
{
	public static class Toolbox
	{
		public const string Name = "Moe Baker";

		public const string Path = Name + "/";
	}
}