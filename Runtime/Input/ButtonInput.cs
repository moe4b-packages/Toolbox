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
    [Serializable]
	public class ButtonInput
	{
        public bool Click { get; protected set; }

        public bool Hold { get; protected set; }

        public bool Lift { get; protected set; }

        public float Time { get; set; }

        public virtual void Process(bool input)
        {
            if (input)
            {
                Time += UnityEngine.Time.deltaTime;

                Click = !Click && !Hold;

                Lift = false;
            }
            else
            {
                Lift = !Lift && Hold;

                Click = false;
            }

            if (Click) Time = 0f;

            Hold = input;
        }
    }
}