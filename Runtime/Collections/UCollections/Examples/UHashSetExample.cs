using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
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
    [AddComponentMenu(Toolbox.Paths.Example + "UHashSet Example")]
    public class UHashSetExample : MonoBehaviour
    {
        public UHashSet<string> hash1;
        public UHashSet<Value> hash2;
        public UHashSet<Component> hash3;
        
        [Serializable]
        public struct Value
        {
            public string first;

            public string last;
        }

        void Start()
        {

        }
    }
}