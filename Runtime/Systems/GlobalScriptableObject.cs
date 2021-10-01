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
    /// A base class for creating a singelton ScriptableObject that will be loaded dynamically from Resources
    /// </summary>
    public abstract class GlobalScriptableObject : ScriptableObject
    {
        public virtual void OnEnable()
        {

        }

        protected virtual void Load()
        {

        }

#if UNITY_EDITOR
        class BuildProcess : IPreprocessBuildWithReport
        {
            public int callbackOrder { get; }

            public void OnPreprocessBuild(BuildReport report)
            {
                var set = new HashSet<Object>(PlayerSettings.GetPreloadedAssets());

                var assets = AssetCollection.Query<GlobalScriptableObject>();
                set.UnionWith(assets);

                PlayerSettings.SetPreloadedAssets(set.ToArray());
            }
        }
#endif
    }

    public class GlobalScriptableObject<T> : GlobalScriptableObject
        where T : GlobalScriptableObject<T>
    {
        public static T Instance { get; protected set; }

        public override void OnEnable()
        {
            base.OnEnable();

            Instance = this as T;

            Debug.LogError($"Loading {name}");

            Load();
        }
    }
}