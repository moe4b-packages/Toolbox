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
    public class GenericComparer<T> : IComparer<T>
    {
        public MethodDelegate Method { get; private set; }
        public delegate int MethodDelegate(T x, T y);

        public int Compare(T x, T y) => Method(x, y);

        public GenericComparer(MethodDelegate method)
        {
            this.Method = method;
        }
    }
}