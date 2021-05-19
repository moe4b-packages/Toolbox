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

[assembly: AssemblySymbolDefine("Assembly_Define_Example")]

namespace MB
{
    public class AssemblySymbolDefineExample : MonoBehaviour
    {
        void Start()
        {
#if Assembly_Define_Example
            Debug.Log("Assembly Define Example Working");
#else
            Debug.LogWarning("Assembly Define Example Not Working");
#endif
        }
    }
}