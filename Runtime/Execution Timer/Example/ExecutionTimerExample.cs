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
	public class ExecutionTimerExample : MonoBehaviour
	{
        void Start()
        {
            ExecutionTimer.LogMethod = Debug.LogWarning;

            ExecutionTimer.Start("Step 1");
            HeavyTask();
            ExecutionTimer.Stop();

            using (ExecutionTimer.New + "Step 2")
            {
                HeavyTask();
            }

            ExecutionTimer.Measure(HeavyTask, "Step 3");
        }

        void HeavyTask()
        {
            var iterations = Random.Range(10_000_000, 50_000_000);

            for (int i = 0; i < iterations; i++)
            {

            }
        }
    }
}