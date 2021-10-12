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
    [RequireComponent(typeof(ExecutionRelay))]
    [AddComponentMenu(ExecutionRelay.Paths.Utility + "Invoke Relay On Key")]
	public class InvokeRelayOnKey : MonoBehaviour, IInitialize
    {
        [SerializeField]
        private KeyCode[] keys = new KeyCode[] { KeyCode.Escape };
        public KeyCode[] Keys => keys;

        ExecutionRelay relay;

        public void Configure()
        {
            relay = GetComponent<ExecutionRelay>();
        }

        public void Initialize() { }

        void Update()
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if(Input.GetKeyDown(keys[i]))
                {
                    relay.Invoke();
                    break;
                }
            }
        }
    }
}