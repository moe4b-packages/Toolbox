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
    public static class AutoPrefs
    {
        public const string ID = nameof(AutoPrefs);

        public static JsonSerializer Serializer { get; private set; }

        public static JObject Context { get; private set; }

        public static bool IsDirty { get; private set; }

        public static class IO
        {
            public const string Name = ID + ".json";

            public static Element Editor { get; private set; }
            public static Element Runtime { get; private set; }

            public static Element Selection => Application.isEditor ? Editor : Runtime;

            public class Element
            {
                public string Path { get; private set; }

                public bool Exists => File.Exists(Path);

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

                public Element(string directory)
                {
                    Path = System.IO.Path.Combine(directory, Name);
                }
            }

            public static void Delete()
            {
                if (Application.isEditor)
                    Editor.Delete();

                Runtime.Delete();
            }

            public static void Save(string text)
            {
                if (Application.isEditor)
                    Editor.Save(text);

                Runtime.Save(text);
            }

            public static string Load() => Selection.Load();

            static IO()
            {
                Editor = new Element(Application.dataPath);
                Runtime = new Element(Application.persistentDataPath);
            }
        }

        public static class Cache
        {
            public static Dictionary<string, object> Dictionary { get; private set; }

            public static bool Contains(string key) => Dictionary.ContainsKey(key);

            public static void Set<T>(string key, T value) => Dictionary[key] = value;

            public static bool TryGetValue<T>(string key, out T value)
            {
                if (Dictionary.TryGetValue(key, out var instance) == false)
                {
                    value = default;
                    return false;
                }

                value = (T)instance;
                return true;
            }

            public static bool Remove(string key) => Dictionary.Remove(key);

            public static void Clear() => Dictionary.Clear();

            static Cache()
            {
                Dictionary = new Dictionary<string, object>();
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

        static bool ConfigurationFlags = false;

        public static void Configure(params JsonConverter[] converters)
        {
            var settings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                Converters = converters,
            };

            Configure(settings);
        }
        public static void Configure(JsonSerializerSettings settings)
        {
            if (ConfigurationFlags)
            {
                Debug.LogWarning($"{ID} is Already Configured");
                return;
            }

            ConfigurationFlags = true;

            Serializer = JsonSerializer.Create(settings);

            Application.quitting += QuitCallback;
        }

        static void QuitCallback()
        {
            if (AutoSave.OnExit && IsDirty)
                Save();
        }

        public static void Reset()
        {
            Context = new JObject();

            Cache.Clear();

            Save();
        }

        public static bool Contains(string key)
        {
            if (Cache.Contains(key)) return true;

            return Context[key] != null;
        }

        public static void Set<T>(string key, T value)
        {
            try
            {
                var token = JToken.FromObject(value, Serializer);

                Context[key] = token;
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot Serialize '{value}' of Type {typeof(T)} to JSON" +
                    $"{Environment.NewLine}" +
                    $"Exception: {ex}");
            }

            Cache.Set(key, value);

            IsDirty = true;

            if (AutoSave.OnChange) Save();
        }

        public static void Read<T>(string key, out T value) => value = Read<T>(key);
        public static void Read<T>(string key, T fallback, out T value) => value = Read<T>(key, fallback);

        public static T Read<T>(string key) => Read<T>(key, default);
        public static T Read<T>(string key, T fallback)
        {
            try
            {
                if (Cache.TryGetValue<T>(key, out var value))
                    return value;
            }
            catch (Exception ex)
            {
                throw FormatException(ex);
            }

            if (Contains(key) == false) return fallback;

            try
            {
                var token = Context[key];

                var value = token.ToObject<T>(Serializer);

                Cache.Set(key, value);

                return value;
            }
            catch (Exception ex)
            {
                throw FormatException(ex);
            }

            Exception FormatException(Exception inner)
            {
                return new Exception($"Cannot Read PlayerPrefX Key '{key}' as {typeof(T)}" +
                    $"{Environment.NewLine}" +
                    $"Exception: {inner}", inner);
            }
        }

        public static bool Remove(string key)
        {
            if (Contains(key) == false) return false;

            Context.Remove(key);
            Cache.Remove(key);

            if (AutoSave.OnChange) Save();

            return true;
        }

        public static void Save()
        {
            var json = Context.ToString(Formatting.Indented);
            IO.Save(json);

            IsDirty = false;
        }

        public static void Load()
        {
            var json = IO.Load();

            Load(json);
        }
        public static void Load(string json)
        {
            if (json == null || json == string.Empty)
            {
                Context = new JObject();
                return;
            }

            try
            {
                Context = JObject.Parse(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot Parse {ID} from '{IO.Selection.Path}'" +
                    $"{Environment.NewLine}" +
                    $"Exception: {ex}", ex);
            }
        }
    }
}