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
    public class UHashSetExample : MonoBehaviour
    {
        public UHashSet1 hash1;
        public UHashSet2 hash2;
        public UHashSet3 hash3;
        
        public Nested nested;
        [Serializable]
        public class Nested
        {
            public UHashSet1 hash1;
            public UHashSet2 hash2;
            public UHashSet3 hash3;
        }

        [Serializable]
        public class UHashSet1 : UHashSet<string> { }

        [Serializable]
        public class UHashSet2 : UHashSet<Value> { }

        [Serializable]
        public class UHashSet3 : UHashSet<Component> { }

        [Serializable]
        public struct Value
        {
            public string firstName;

            public string lastName;
        }

        void Start()
        {

        }
    }
}