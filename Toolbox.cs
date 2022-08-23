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

using System.Reflection;
using Newtonsoft.Json.Utilities;

[assembly: AssemblySymbolDefine("MOE_TOOLBOX")]

namespace MB
{
    public static class Toolbox
    {
        public const string Name = "Moe Baker";

        public static class Paths
        {
            public const string Root = Name + "/";

            public const string Box = Root + "Toolbox/";

            public const string Rewind = Box + "Rewind/";

            public const string Misc = Box + "Misc/";

            public const string Example = Misc + "Example/";
        }

        public static class IO
        {
            const string RuntimeDirectory = "Assets/Moe Baker/";

            public static string GenerateRuntimePath(string segment) => Path.Combine(RuntimeDirectory, segment);
        }

        [RuntimeInitializeOnLoadMethod]
        static void OnLoad()
        {
            AotHelper.EnsureList<KeyValuePair<string, string>>();
        }
    }
}