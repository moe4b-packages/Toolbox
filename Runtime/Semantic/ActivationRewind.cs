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
    [AddComponentMenu(Toolbox.Paths.Rewind + "Activation Rewind")]
    public class ActivationRewind : MonoBehaviour
	{
        [SerializeField]
        UnityEvent enableEvent = default;
        public UnityEvent EnableEvent => enableEvent;
        protected virtual void OnEnable()
        {
            enableEvent?.Invoke();
        }

        [SerializeField]
        UnityEvent disableEvent = default;
        public UnityEvent DisableEvent => disableEvent;
        protected virtual void OnDisable()
        {
            disableEvent?.Invoke();
        }
    }
}