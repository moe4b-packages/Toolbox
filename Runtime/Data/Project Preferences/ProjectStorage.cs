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
using Newtonsoft.Json.Linq;

namespace MB
{
    /// <summary>
    /// Persistent data storage class that will persist from editor to build
    /// </summary>
    [Manager]
    [SettingsMenu(Toolbox.Paths.Root + "Project Storage")]
    [LoadOrder(Runtime.Defaults.LoadOrder.ProjectStorage)]
    public class ProjectStorage : ScriptableManager<ProjectStorage>
    {
        [SerializeField]
        [TextArea(55, 400)]
        string json = default;
        public string Json => json;

        public JObjectComposer Composer { get; private set; }

        protected override void OnLoad()
        {
            base.OnLoad();

            Composer = JObjectComposer.Create<ProjectStorage>();
            var settings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
            };
            Composer.Configure(settings);
            Composer.Load(json);

            Composer.OnChange += ComposerChangeCallback;
        }

        void OnValidate()
        {
            if (Composer == null) return;

            try
            {
                var context = JObject.Parse(json);

                if (Composer.Context == context)
                    return;
            }
            catch (Exception)
            {
                return;
            }

            Composer.Load(json);
        }

        public void Set(string key, object value) => Composer.Set(key, value);
        public T Read<T>(string key, T fallback = default) => Composer.Read(key, fallback: fallback);
        public bool Contains(string key) => Composer.Contains(key);
        public bool Remove(string key) => Composer.Remove(key);

        void ComposerChangeCallback()
        {
            json = Composer.Context.ToString();

#if UNITY_EDITOR
            Runtime.Save(this);
#endif
        }
    }
}