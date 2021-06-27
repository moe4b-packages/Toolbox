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
    [AddComponentMenu(Toolbox.Path + "Animation Trigger Rewind")]
    public class AnimationTriggerRewind : MonoBehaviour
	{
        [SerializeField]
        List<Entry> entries = default;
        public List<Entry> Entries => entries;
        [Serializable]
        public class Entry
        {
            [SerializeField]
            string _ID = default;
            public string ID => _ID;

            [SerializeField]
            UnityEvent onInvoke = default;
            public UnityEvent OnInvoke => onInvoke;

            internal void Trigger()
            {
                onInvoke?.Invoke();
            }
        }

        public Dictionary<string, Entry> Dictionary { get; protected set; }
        void ParseDictionary()
        {
            Dictionary = entries.ToDictionary(x => x.ID);
        }

        public delegate void TriggerDelegate(string id);
		public event TriggerDelegate OnTrigger;
		public void Trigger(string id)
        {
			OnTrigger?.Invoke(id);

            if (Dictionary.TryGetValue(id, out var entry))
                entry.Trigger();
        }

        void Awake()
        {
            ParseDictionary();
        }

        void OnValidate()
        {
            ParseDictionary();
        }
    }
}