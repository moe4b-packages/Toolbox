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
    [AddComponentMenu(Toolbox.Paths.Example + "UDictionary Example")]
    public class UDictionaryExample : MonoBehaviour
    {
        public UDictionary<string, string> dictionary1;
        public UDictionary<Key, Value> dictionary2;
        public UDictionary<Component, Vector3> dictionary3;

        public Nested nested;
        [Serializable]
        public class Nested
        {
            public UDictionary<string, string> dictionary1;
            public UDictionary<Key, Value> dictionary2;
            public UDictionary<Component, Vector3> dictionary3;
        }

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