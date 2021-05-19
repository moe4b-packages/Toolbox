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

using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace MB
{
	public static class ManualLateStart
	{
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnLoad()
        {
            MUtility.RegisterPlayerLoop<PreLateUpdate>(PreLateUpdate);
        }

        public static Queue<Action> Queue { get; private set; }

        public static void Register(Action callback) => Queue.Enqueue(callback);

        static void PreLateUpdate()
        {
            while (Queue.Count > 0)
            {
                var callback = Queue.Dequeue();

                if (callback == null) continue;

                callback();
            }
        }

        static ManualLateStart()
        {
            Queue = new Queue<Action>();
        }
    }
}