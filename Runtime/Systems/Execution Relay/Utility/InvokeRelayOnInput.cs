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
	public class InvokeRelayOnInput : MonoBehaviour, IInitialize
	{
		[SerializeField]
        KeyCode[] keys = new KeyCode[] { KeyCode.Escape, KeyCode.Home };
        public KeyCode[] Keys => keys;

        ExecutionRelay relay;

        public void Configure()
        {
            relay = GetComponent<ExecutionRelay>();
        }

        public void Init() { }

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