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
    [AddComponentMenu(Toolbox.Paths.Rewind + "Game Object Activation Rewind")]
    public class GameObjectActivationRewind : MonoBehaviour
	{
        public event Action EnableEvent;
        protected virtual void OnEnable()
        {
            EnableEvent?.Invoke();
        }

        public event Action DisableEvent;
        protected virtual void OnDisable()
        {
            DisableEvent?.Invoke();
        }
    }
}