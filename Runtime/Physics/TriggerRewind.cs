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
    [AddComponentMenu(Toolbox.Path + "Trigger Rewind")]
	public class TriggerRewind : MonoBehaviour
	{
        [SerializeField]
        UnityEvent<Collider> onEnter = default;
        public UnityEvent<Collider> OnEnter => onEnter;
        void OnTriggerEnter(Collider context)
        {
            onEnter?.Invoke(context);
        }

        [SerializeField]
        UnityEvent<Collider> onStay = default;
        public UnityEvent<Collider> OnStay => onStay;
        void OnTriggerStay(Collider context)
        {
            onStay?.Invoke(context);
        }

        [SerializeField]
        UnityEvent<Collider> onExit = default;
        public UnityEvent<Collider> OnExit => onExit;
        void OnTriggerExit(Collider context)
        {
            onExit?.Invoke(context);
        }
    }
}