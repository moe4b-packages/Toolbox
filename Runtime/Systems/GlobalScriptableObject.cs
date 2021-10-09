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

using UnityEditor.Compilation;

using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

using System.Reflection;

namespace MB
{
    /// <summary>
    /// A base class for creating a singleton ScriptableObject that will be loaded dynamically from Resources
    /// </summary>
    public abstract class GlobalScriptableObject : ScriptableObject
    {
        protected virtual void Awake()
        {
            #if UNITY_EDITOR
            PreloadedAssets.Add(this);
            #endif
        }

        protected virtual void OnEnable()
        {

        }

        protected virtual void Load()
        {

        }

        protected virtual void OnDestroy()
        {
            #if UNITY_EDITOR
            PreloadedAssets.Remove(this);
            #endif
        }

        #if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void OnEditorLoad()
        {
            LoadAll();
        }

        static void LoadAll()
        {
            //Ironically, pre-loaded assets are only preloaded in the build, not in the editor
            //so this ensures that they are loaded on editor
            using (PreloadedAssets.Lease(out var set))
            {
                var assets = AssetCollection.Query<GlobalScriptableObject>();
                set.UnionWith(assets);
            }
        }

        private class PreBuildProcessor : IPreprocessBuildWithReport
        {
            public int callbackOrder => 0;

            public void OnPreprocessBuild(BuildReport report) => LoadAll();
        }
        #endif
    }

    public class GlobalScriptableObject<T> : GlobalScriptableObject
        where T : GlobalScriptableObject<T>
    {
        public static T Instance { get; protected set; }

        protected override void OnEnable()
        {
            base.OnEnable();

            Instance = this as T;

            Load();
        }
    }
}