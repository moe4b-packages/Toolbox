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
        [SerializeField]
        List<Entry> entries = default;
        public List<Entry> Entries => entries;
        [Serializable]
        public class Entry
        {
            [SerializeField]
            string _ID = default;
            public string ID
            {
                get => _ID;
                set => _ID = value;
            }

            [SerializeField]
            UnityEvent onInvoke = default;
            public UnityEvent OnInvoke => onInvoke;

            internal void Trigger()
            {
                onInvoke?.Invoke();
            }

            public Entry()
            {
                ID = string.Empty;
            }
        }

        public Dictionary<string, Entry> Dictionary { get; protected set; }
        public Entry this[string id] => Dictionary[id];

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
            Dictionary = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in entries)
                Dictionary[entry.ID] = entry;
        }
    }
}