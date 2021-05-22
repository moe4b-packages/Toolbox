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
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MB
{
#if UNITY_EDITOR
    /// <summary>
    /// A PreProcessor for scripts, used for replacing #VARIABLES like #NAMESPACE
    /// </summary>
    public class ScriptCreationPreprocessor : UnityEditor.AssetModificationProcessor
    {
        public static List<AssemblyData> Assemblies { get; protected set; }
        public class AssemblyData
        {
            public string Directory { get; protected set; }

            public string RootNamespace { get; protected set; }

            public AssemblyDefinitionAsset Asset { get; protected set; }

            public AssemblyData(string directory, AssemblyDefinitionAsset asset)
            {
                this.Directory = directory;
                this.Asset = asset;

                var token = JObject.Parse(asset.text)["rootNamespace"];

                RootNamespace = token == null ? null : token.ToObject<string>();
            }
        }

        public static bool TryGetAssembly(string file, out AssemblyData assembly)
        {
            var directory = new FileInfo(file).Directory.FullName;

            for (int i = 0; i < Assemblies.Count; i++)
            {
                if (directory.Contains(Assemblies[i].Directory))
                {
                    assembly = Assemblies[i];
                    return true;
                }
            }

            assembly = null;
            return false;
        }

        public static string GlobalNamespace
        {
            get
            {
                var value = EditorSettings.projectGenerationRootNamespace;

                if (string.IsNullOrEmpty(value)) value = "Default";

                return value;
            }
        }

        public static void OnWillCreateAsset(string path)
        {
            path = path.Replace(".meta", "");

            var extension = Path.GetExtension(path);
            if (extension != ".cs") return;

            var text = File.ReadAllText(path);

            text = ProcessNamespace(path, text);

            File.WriteAllText(path, text);

            AssetDatabase.Refresh();
        }

        static string ProcessNamespace(string path, string text)
        {
            var name = GlobalNamespace;

            if (TryGetAssembly(path, out var assembly))
                if (string.IsNullOrEmpty(assembly.RootNamespace) == false)
                    name = assembly.RootNamespace;

            return text.Replace("#NAMESPACE#", name);
        }

        static ScriptCreationPreprocessor()
        {
            Assemblies = new List<AssemblyData>();

            var guids = AssetDatabase.FindAssets($"t:{nameof(AssemblyDefinitionAsset)}");

            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);

                var file = new FileInfo(path);
                var directory = file.Directory.FullName;

                var asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);

                var data = new AssemblyData(directory, asset);

                Assemblies.Add(data);
            }
        }
    }
#endif
}