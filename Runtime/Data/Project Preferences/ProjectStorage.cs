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
    /// Persistent data storage class that can be accessed from both editor and player
    /// </summary>
    [Manager]
    [SettingsMenu(Toolbox.Paths.Root + "Project Storage")]
    [LoadOrder(Runtime.Defaults.LoadOrder.ProjectStorage)]
    public class ProjectStorage : ScriptableManager<ProjectStorage>
    {
        //Instance
        #region
        [SerializeField]
        [TextArea(55, 400)]
        string json = default;

        protected override void OnLoad()
        {
            base.OnLoad();

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
            Runtime.Save(this);
#endif
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
        #endregion

        //Static
        #region
        public static string Json => Instance.json;

        public static JObjectComposer Composer { get; private set; }

        public static void Set(string key, object value) => Composer.Set(key, value);
        public static T Read<T>(string key, T fallback = default) => Composer.Read(key, fallback: fallback);
        public static bool Contains(string key) => Composer.Contains(key);
        public static bool Remove(string key) => Composer.Remove(key);

        static ProjectStorage()
        {
            Composer = JObjectComposer.Create<ProjectStorage>();
        }
        #endregion
    }
}