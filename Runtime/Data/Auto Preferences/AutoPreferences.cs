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
    /// More competant version of PlayerPrefs, uses JsonDotNet, can serialize anything,
    /// needs to be configured and loaded before use
    /// </summary>
    [Manager]
    [SettingsMenu(Toolbox.Paths.Root + ID)]
    [LoadOrder(Runtime.Defaults.LoadOrder.AutoPreferences)]
    public class AutoPreferences : ScriptableManager<AutoPreferences>
    {
        //Instance
        #region
        [SerializeField]
        [SerializedType.Selection(typeof(JsonConverter))]
        SerializedType[] converters = new SerializedType[]
        {
            typeof(CustomJsonConveters.IPAddressConverter),
            typeof(CustomJsonConveters.ColorConverter),
        };

        protected override void OnLoad()
        {
            base.OnLoad();

            if (IsInitialized == false) Initialize();
        }
        #endregion

        //Static
        #region
        public const string ID = "Auto Preferences";
        public const string Path = Toolbox.Paths.Box + ID + "/";

        public static SerializedType[] Converters => Instance.converters;
        static JsonConverter[] CreateConverters()
        {
            var array = new JsonConverter[Converters.Length];

            for (int i = 0; i < array.Length; i++)
                array[i] = Activator.CreateInstance(Converters[i]) as JsonConverter;

            return array;
        }

        public static JObjectComposer Composer { get; private set; }

        public static bool IsInitialized { get; private set; }

        public static bool IsDirty { get; private set; }

        public static class IO
        {
            public const string FileName = ID + ".json";

            public static string Path { get; private set; }
            public static bool Exists => File.Exists(Path);

            internal static void Prepare()
            {
                var directory = Application.isEditor ? Application.dataPath : Application.persistentDataPath;

                Path = System.IO.Path.Combine(directory, FileName);
            }

            public static void Save(string text)
            {
                File.WriteAllText(Path, text);
            }

            public static void Delete()
            {
                if (Exists == false) return;

                File.Delete(Path);
            }

            public static string Load()
            {
                if (Exists == false) return string.Empty;

                var text = File.ReadAllText(Path);

                return text;
            }
        }

        public static class Context
        {
            internal static void Load()
            {
                try
                {
                    var json = IO.Load();

                    Composer.Load(json);
                }
                catch (Exception)
                {
                    Debug.LogWarning($"{ID} Couldn't be Loaded Succesfully, Resetting");
                    Reset();
                }
            }

            public static void Reset()
            {
                Composer.Clear();

                Save();
            }

            public static void Save()
            {
                var json = Composer.Read();
                IO.Save(json);

                IsDirty = false;
            }
        }

        public static class AutoSave
        {
            public static bool OnChange { get; set; } = true;
            public static bool OnExit { get; set; } = true;

            public static bool All
            {
                set
                {
                    OnChange = value;
                    OnExit = value;
                }
            }
        }

        static void Initialize()
        {
            IO.Prepare();

            var settings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                Converters = CreateConverters()
            };
            Composer.Configure(settings);

            Context.Load();

            Composer.OnChange += ComposerChangeCallback;

            Application.quitting += ApplicationQuitCallback;

            IsInitialized = true;
        }

        static void ComposerChangeCallback()
        {
            if (AutoSave.OnChange)
                Context.Save();
            else
                IsDirty = true;
        }

        public static bool Contains(string key) => Composer.Contains(key);
        public static void Set(string key, object value) => Composer.Set(key, value);
        public static T Read<T>(string key, T fallback = default) => Composer.Read(key, fallback: fallback);
        public static object Read(Type data, string key, object fallback = default) => Composer.Read(key, data, fallback);
        public static bool Remove(string key) => Composer.Remove(key);

        static void ApplicationQuitCallback()
        {
            if (AutoSave.OnExit && IsDirty)
                Context.Save();
        }

        static AutoPreferences()
        {
            Composer = JObjectComposer.Create<AutoPreferences>();
        }
        #endregion
    }
}