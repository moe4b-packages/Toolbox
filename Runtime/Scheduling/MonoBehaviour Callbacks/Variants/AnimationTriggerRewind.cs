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
    public class AnimationTriggerRewind : MonobehaviourCallback.Processor<AnimationTriggerRewind>
    {
        public Dictionary<string, HashSet<Action>> Dictionary { get; private set; }

        public delegate void TriggerDelegate(string id);
        public event TriggerDelegate OnTrigger;
        public void Trigger(string id)
        {
            OnTrigger?.Invoke(id);

            if (Dictionary.TryGetValue(id, out var set))
            {
                foreach (var callback in set)
                    CallbackCache.Add(callback);

                for (int i = 0; i < CallbackCache.Count; i++)
                    CallbackCache[i].Invoke();

                CallbackCache.Clear();
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

        public AnimationTriggerRewind()
        {
            Dictionary = new Dictionary<string, HashSet<Action>>();
        }

        //Static Utility
        static List<Action> CallbackCache = new List<Action>();
    }
}