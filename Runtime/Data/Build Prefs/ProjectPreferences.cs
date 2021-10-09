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
    [Global(ScriptableManagerScope.Project)]
    [SettingsMenu(Toolbox.Path + "Preferences")]
	public class ProjectPreferences : ScriptableManager<ProjectPreferences>
	{
        [SerializeField]
        [TextArea(55, 400)]
        string json = default;
        public string Json => json;

        public JObjectComposer Composer { get; protected set; }

        protected override void Load()
        {
            base.Load();

            Composer = new JObjectComposer();

            var settings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
            };

            Composer.Configure(settings);
            Composer.Load(json);

            Composer.OnChange += ComposerChangeCallback;
        }

        void ComposerChangeCallback()
        {
            json = Composer.Context.ToString();

            #if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            #endif
        }

        public void Set(string key, object value) => Composer.Set(key, value);

        public T Read<T>(string key, T fallback = default) => Composer.Read(key, fallback: fallback);

        public bool Contains(string key) => Composer.Contains(key);

        public bool Remove(string key) => Composer.Remove(key);
    }
}