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
    /// requires .Net 4.X compatibility on Windows (and possibly other platforms too)
    /// instead of .Net 2.0 Standard because of a bug? with JsonDotNet I think
    /// </summary>
    public static class AutoPrefs
    {
        public const string ID = nameof(AutoPrefs);

        public static JObjectComposer Composer { get; private set; }

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

                public bool Obfuscate { get; private set; }

                public bool Exists => File.Exists(Path);

                public void Save(string text)
                {
                    if (Obfuscate) text = Obfuscation.Encrypt(text);

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

                    if (Obfuscate) text = Obfuscation.Decrypt(text);

                    return text;
                }

                public Element(string directory, bool obfuscate)
                {
                    Path = System.IO.Path.Combine(directory, Name);
                    this.Obfuscate = obfuscate;
                }
            }

            public static void Delete()
            {
#if UNITY_EDITOR
                Editor.Delete();
#endif

                Runtime.Delete();
            }

            public static void Save(string text)
            {
#if UNITY_EDITOR
                Editor.Save(text);
#endif

                Runtime.Save(text);
            }

            public static string Load() => Selection.Load();

            static IO()
            {
                Editor = new Element(Application.dataPath, false);
                Runtime = new Element(Application.persistentDataPath, true);
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

        public static class Obfuscation
        {
            public static void SetMethods(EncryptDelegate encrypt, DecryptDelegate decrypt)
            {
                Encrypt = encrypt;
                Decrypt = decrypt;
            }

            public static EncryptDelegate Encrypt { get; set; } = DefaultEncryptMethod;
            public delegate string EncryptDelegate(string text);
            public static string DefaultEncryptMethod(string text) => text;

            public static DecryptDelegate Decrypt { get; set; } = DefaultDecryptMethod;
            public delegate string DecryptDelegate(string text);
            public static string DefaultDecryptMethod(string text) => text;
        }

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
            if (Composer.IsConfigured)
            {
                Debug.LogWarning($"{ID} is Already Configured");
                return;
            }

            Composer.Configure(settings);
            Composer.OnChange += InvokeChange;

            Application.quitting += QuitCallback;
        }

        public static void Load()
        {
            var json = IO.Load();

            Load(json);
        }
        public static void Load(string json)
        {
            Composer.Load(json);
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

        static void InvokeChange()
        {
            if (AutoSave.OnChange)
                Save();
            else
                IsDirty = true;
        }

        #region Controls
        public static bool Contains(string key) => Composer.Contains(key);

        public static void Set<T>(string key, T value) => Composer.Set(key, value);

        public static T Read<T>(string key, T fallback = default)
        {
            return Composer.Read(key, fallback: fallback);
        }
        public static object Read(Type data, string key, object fallback = default)
        {
            return Composer.Read(data, key, fallback);
        }

        public static bool Remove(string key) => Composer.Remove(key);
        #endregion

        static void QuitCallback()
        {
            if (AutoSave.OnExit && IsDirty)
                Save();
        }

        static AutoPrefs()
        {
            Composer = new JObjectComposer();
        }
    }
}