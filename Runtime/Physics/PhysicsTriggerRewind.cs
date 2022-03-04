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
    [AddComponentMenu(Toolbox.Paths.Rewind + "Physics Trigger Rewind")]
	public class PhysicsTriggerRewind : MonoBehaviour
	{
        public delegate void EventDelegate(Collider collider);

        public event EventDelegate EnterEvent;
        void OnTriggerEnter(Collider collider)
        {
            EnterEvent?.Invoke(collider);
        }

        public event EventDelegate StayEvent;
        void OnTriggerStay(Collider collider)
        {
            StayEvent?.Invoke(collider);
        }

        public event EventDelegate ExitEvent;
        void OnTriggerExit(Collider collider)
        {
            ExitEvent?.Invoke(collider);
        }
    }
}