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
    [Manager]
    [SettingsMenu(Toolbox.Paths.Root + "Preferences")]
    [LoadOrder(Runtime.Defaults.LoadOrder.ProjectPreferences)]
    public class ProjectPreferences : ScriptableManager<ProjectPreferences>
    {
        [SerializeField]
        [TextArea(55, 400)]
        string json = default;
        public static string Json => Instance.json;

        public static JObjectComposer Composer { get; protected set; }

        protected override void OnLoad()
        {
            base.OnLoad();

            var settings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
            };

            Composer = new JObjectComposer(settings);
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

        #region Controls
        public static void Set(string key, object value) => Composer.Set(key, value);

        public static T Read<T>(string key, T fallback = default) => Composer.Read(key, fallback: fallback);

        public static bool Contains(string key) => Composer.Contains(key);

        public static bool Remove(string key) => Composer.Remove(key);
        #endregion
    }
}