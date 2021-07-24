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

using Newtonsoft.Json;

namespace MB
{
	public class BuildPrefs : GlobalScriptableObject<BuildPrefs>
	{
        [SerializeField]
        [TextArea]
        [HideInInspector]
        string json = default;
        public string Json => json;

        public JObjectComposer Composer { get; protected set; }

        void ChangeCallback()
        {
            json = Composer.Context.ToString();
        }

        public void Set(string key, object value) => Composer.Set(key, value);

        public T Read<T>(string key, T fallback = default) => Composer.Read<T>(key, fallback: fallback);

        public bool Contains(string key) => Composer.Contains(key);

        public bool Remove(string key) => Composer.Remove(key);

        public BuildPrefs()
        {
            Composer = new JObjectComposer();

            var settings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
            };

            Composer.Configure(settings);
            Composer.Load(json);

            Composer.OnChange += ChangeCallback;
        }
    }
}