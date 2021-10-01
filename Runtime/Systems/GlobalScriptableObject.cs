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
        protected virtual void Awake()
        {
#if UNITY_EDITOR
            PreloadAssets.Add(this);
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
            PreloadAssets.Remove(this);
#endif
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void OnEditorLoad()
        {
            ///Ironically, pre-loaded assets are only preloaded in the build, not in the editor
            ///so this ensures that they are loaded on editor
            PreloadAssets.Update();
        }

        class PreBuildProcessor : IPreprocessBuildWithReport
        {
            public int callbackOrder => 0;

            public void OnPreprocessBuild(BuildReport report) => PreloadAssets.Update();
        }

        static class PreloadAssets
        {
            public static Object[] Array
            {
                get => PlayerSettings.GetPreloadedAssets();
                set => PlayerSettings.SetPreloadedAssets(value);
            }

            public static bool Add(ScriptableObject target)
            {
                using (DisposablePool.HashSet<Object>.Lease(out var set))
                {
                    set.UnionWith(Array);

                    if (set.Add(target) == false)
                        return false;

                    Clean(set);

                    Array = set.ToArray();
                    return true;
                }
            }

            public static bool Remove(ScriptableObject target)
            {
                using (DisposablePool.HashSet<Object>.Lease(out var set))
                {
                    set.UnionWith(Array);

                    if (set.Remove(target) == false)
                        return false;

                    Clean(set);

                    Array = set.ToArray();
                    return true;
                }
            }

            public static void Update()
            {
                using (DisposablePool.HashSet<Object>.Lease(out var set))
                {
                    set.UnionWith(Array);

                    var assets = AssetCollection.Query<GlobalScriptableObject>();
                    set.UnionWith(assets);

                    Clean(set);

                    Array = set.ToArray();
                }
            }

            public static void Clean()
            {
                using (DisposablePool.HashSet<Object>.Lease(out var set))
                {
                    set.UnionWith(Array);

                    Clean(set);

                    Array = set.ToArray();
                }
            }
            internal static void Clean(HashSet<Object> set)
            {
                set.RemoveWhere(IsNull);
                bool IsNull(Object target) => target == null;
            }
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