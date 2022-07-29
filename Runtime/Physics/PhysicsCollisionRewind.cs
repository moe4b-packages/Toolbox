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

using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MB
{
    [AddComponentMenu(Toolbox.Paths.Rewind + "Physics Collision Rewind")]
	public class PhysicsCollisionRewind : MonoBehaviour
    {
        public delegate void EventDelegate(Collision collision);

        public event EventDelegate EnterEvent;
        void OnCollisionEnter(Collision collision)
        {
            EnterEvent?.Invoke(collision);
        }

        public event EventDelegate StayEvent;
        void OnCollisionStay(Collision collision)
        {
            StayEvent?.Invoke(collision);
        }

        public event EventDelegate ExitEvent;
        void OnCollisionExit(Collision collision)
        {
            ExitEvent?.Invoke(collision);
        }

        public static PhysicsCollisionRewind Retrieve(UObjectSurrogate target) => MonobehaviourCallback.Retrieve<PhysicsCollisionRewind>(target);
    }
}