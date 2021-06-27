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
    [AddComponentMenu(Toolbox.Path + "Collision Rewind")]
	public class CollisionRewind : MonoBehaviour
    {
        [SerializeField]
        UnityEvent<Collision> onEnter = default;
        public UnityEvent<Collision> OnEnter => onEnter;
        void OnCollisionEnter(Collision collision)
        {
            onEnter?.Invoke(collision);
        }

        [SerializeField]
        UnityEvent<Collision> onStay = default;
        public UnityEvent<Collision> OnStay => onStay;
        void OnCollisionStay(Collision collision)
        {
            onStay?.Invoke(collision);
        }

        [SerializeField]
        UnityEvent<Collision> onExit = default;
        public UnityEvent<Collision> OnExit => onExit;
        void OnCollisionExit(Collision collision)
        {
            onExit?.Invoke(collision);
        }
    }
}