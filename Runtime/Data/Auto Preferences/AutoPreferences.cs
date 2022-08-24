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
    /// More competant version of PlayerPrefs, uses JsonDotNet, can serialize anything
    /// </summary>
    [Manager]
    [SettingsMenu(Toolbox.Paths.Root + ID)]
    [LoadOrder(Runtime.Defaults.LoadOrder.AutoPreferences)]
    public class AutoPreferences : ScriptableManager<AutoPreferences>
    {
        public const string ID = "Auto Preferences";

        [SerializeField]
        [SerializedType.Selection(typeof(JsonConverter))]
        SerializedType[] converters = new SerializedType[]
        {
            typeof(CustomJsonConveters.IPAddressConverter),
            typeof(CustomJsonConveters.ColorConverter),
        };
        public SerializedType[] Converters => converters;

        public JObjectComposer Composer { get; private set; }

        public bool IsDirty { get; private set; }

        [field: SerializeField]
        public IOProperty IO { get; private set; }
        [Serializable]
        public class IOProperty
        {
            [field: SerializeField]
            public string FileName { get; private set; } = ID + ".json";

            public string Path { get; private set; }
            public bool Exists => File.Exists(Path);

            internal void Prepare()
            {
                var directory = Application.isEditor ? Application.dataPath : Application.persistentDataPath;

                Path = System.IO.Path.Combine(directory, FileName);
            }

            public void Save(string text)
            {
                File.WriteAllText(Path, text);
            }

            public void Delete()
            {
                if (Exists == false) return;

                File.Delete(Path);
            }

            public string Load()
            {
                if (Exists == false) return string.Empty;

                var text = File.ReadAllText(Path);

                return text;
            }
        }

        [field: SerializeField]
        public AutoSaveProperty AutoSave { get; private set; }
        [Serializable]
        public class AutoSaveProperty
        {
            [field: SerializeField]
            public bool OnChange { get; set; } = true;

            [field: SerializeField]
            public bool OnExit { get; set; } = true;

            public bool All
            {
                set
                {
                    OnChange = value;
                    OnExit = value;
                }
            }
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            IO.Prepare();

            Composer = JObjectComposer.Create<AutoPreferences>();

            var settings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                Converters = CreateConverters()
            };
            Composer.Configure(settings);

            Composer.OnChange += ComposerChangeCallback;

            try
            {
                var json = IO.Load();

                Composer.Load(json);
            }
            catch (Exception)
            {
                Debug.LogWarning($"{ID} Couldn't be Loaded Succesfully, Resetting");
                Clear();
            }

            Application.quitting += ApplicationQuitCallback;

            JsonConverter[] CreateConverters()
            {
                var array = new JsonConverter[Converters.Length];

                for (int i = 0; i < array.Length; i++)
                    array[i] = Activator.CreateInstance(Converters[i]) as JsonConverter;

                return array;
            }
        }

        public void Clear()
        {
            Composer.Clear();

            Save();
        }

        public void Save()
        {
            var json = Composer.Read();
            IO.Save(json);

            IsDirty = false;
        }

        #region Interface
        public bool Contains(string key) => Composer.Contains(key);
        public void Set(string key, object value) => Composer.Set(key, value);
        public T Read<T>(string key, T fallback = default) => Composer.Read(key, fallback: fallback);
        public object Read(Type data, string key, object fallback = default) => Composer.Read(key, data, fallback);
        public bool Remove(string key) => Composer.Remove(key);
        #endregion

        #region Callback
        void ComposerChangeCallback()
        {
            if (AutoSave.OnChange)
                Save();
            else
                IsDirty = true;
        }

        void ApplicationQuitCallback()
        {
            if (AutoSave.OnExit && IsDirty)
                Save();
        }
        #endregion
    }

    public class AutoPreferenceVariable<T>
    {
        AutoPreferences AutoPreferences => AutoPreferences.Instance;

        public string ID { get; }

        T backing;
        public T Value
        {
            get => backing;
            set
            {
                backing = value;

                Save();
            }
        }

        public void Load()
        {
            backing = AutoPreferences.Read(ID, backing);
        }
        public void Save()
        {
            AutoPreferences.Set(ID, backing);
        }

        public override string ToString() => backing.ToString();

        public AutoPreferenceVariable(string ID) : this(ID, default) { }
        public AutoPreferenceVariable(string ID, T initial)
        {
            this.ID = ID;
            this.backing = initial;

            Load();
        }

        public static implicit operator T(AutoPreferenceVariable<T> variable) => variable.Value;
    }
}