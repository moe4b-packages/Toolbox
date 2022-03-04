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

namespace MB
{
    [AddComponentMenu(Toolbox.Paths.Rewind + "Animation Trigger Rewind")]
    public class AnimationTriggerRewind : MonoBehaviour
	{
        public Dictionary<string, HashSet<Action>> Dictionary { get; private set; }

        void Awake()
        {
            Dictionary = new Dictionary<string, HashSet<Action>>();
        }

        public delegate void TriggerDelegate(string id);
        public event TriggerDelegate OnTrigger;
        public void Trigger(string id)
        {
            OnTrigger?.Invoke(id);

            if (Dictionary.TryGetValue(id, out var set))
            {
                foreach (var callback in set)
                    CallbackPool.Add(callback);

                for (int i = 0; i < CallbackPool.Count; i++)
                    CallbackPool[i].Invoke();

                CallbackPool.Clear();
            }
        }

        public bool Register(string id, Action callback)
        {
            if (Dictionary.TryGetValue(id, out var set) == false)
            {
                set = new HashSet<Action>();
                Dictionary[id] = set;
            }

            return set.Add(callback);
        }
        public bool Unregister(string id, Action callback)
        {
            if (Dictionary.TryGetValue(id, out var set) == false)
                return false;

            return set.Remove(callback);
        }

        //Static Utility
        static List<Action> CallbackPool;

        static AnimationTriggerRewind()
        {
            CallbackPool = new List<Action>();
        }
    }
}