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
    /// Executes a method on both run-time (enter play-mode) and edit-time (editor open and code recompile)
    /// </summary>
#if UNITY_EDITOR
    public class InitializeOnAllLoadMethod : InitializeOnLoadMethodAttribute
#else
    public class InitializeOnAllLoadMethod : RuntimeInitializeOnLoadMethodAttribute
#endif
    {
        public InitializeOnAllLoadMethod() : this(RuntimeInitializeLoadType.AfterSceneLoad) { }

#if UNITY_EDITOR
        public InitializeOnAllLoadMethod(RuntimeInitializeLoadType loadType) { }
#else
        public InitializeOnAllLoadMethod(RuntimeInitializeLoadType loadType) : base(loadType) { }
#endif
    }
}