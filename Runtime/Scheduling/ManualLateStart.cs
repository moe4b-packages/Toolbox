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
    /// <summary>
    /// Executed before LateUpdate, but After Update, Register Delegate for it to get Invoked Once
    /// </summary>
	public static class ManualLateStart
	{
        public static Queue<Action> Queue { get; private set; }

        public static void Register(Action callback) => Queue.Enqueue(callback);

        static void PreLateUpdate()
        {
            while (Queue.Count > 0)
                Queue.Dequeue()?.Invoke();
        }

        static ManualLateStart()
        {
            Queue = new Queue<Action>();

            MUtility.RegisterPlayerLoop<PreLateUpdate>(PreLateUpdate);
        }
    }
}