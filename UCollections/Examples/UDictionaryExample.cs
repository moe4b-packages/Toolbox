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
    public class UDictionaryExample : MonoBehaviour
    {
        public UDictionary1 dictionary1;
        public UDictionary2 dictionary2;
        public UDictionary3 dictionary3;

        public Nested nested;
        [Serializable]
        public class Nested
        {
            public UDictionary1 dictionary1;
            public UDictionary2 dictionary2;
            public UDictionary3 dictionary3;
        }

        [Serializable]
        public class UDictionary1 : UDictionary<string, string> { }

        [Serializable]
        public class UDictionary2 : UDictionary<Key, Value> { }

        [Serializable]
        public class UDictionary3 : UDictionary<Component, Vector3> { }

        [Serializable]
        public struct Key
        {
            public string ID;

            public string file;
        }

        [Serializable]
        public struct Value
        {
            public string firstName;

            public string lastName;
        }

        void Start()
        {
            dictionary1["See Ya Later"] = "Space Cowboy";
        }
    }
}