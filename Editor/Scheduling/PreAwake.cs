using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
#endif

using UnityEngine;
using UnityEngine.SceneManagement;

namespace MB
{
    /// <summary>
    /// An editor only system that implements a PreAwake callback that can be used to cache some Awake operations
    /// (retrieving components, setting up childern, ... etc)
    /// </summary>
    public static class PreAwake
    {
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void OnLoad()
        {
            ProcessAssets();
        }

        class PreProcessBuild : IPreprocessBuildWithReport
        {
            public int callbackOrder => -400;

            public void OnPreprocessBuild(BuildReport report)
            {
                PreAwake.ProcessAssets();
            }
        }

        internal static void ProcessAssets()
        {
            foreach (var asset in AssetCollection.List)
            {
                switch (asset)
                {
                    case GameObject gameObject:
                        using (ComponentQuery.Collection.NonAlloc.InHierarchy<IInterface>(gameObject, out var list))
                        {
                            foreach (var item in list)
                            {
                                Process(item);
                            }
                        }
                        break;

                    case IInterface context:
                        Process(context);
                        break;
                }
            }
        }

        [PostProcessScene]
        static void PostProcessScene()
        {
            var scene = SceneManager.GetActiveScene();

            using (ComponentQuery.Collection.NonAlloc.InScene<IInterface>(scene, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                    Process(list[i]);
            }
        }

        internal static void Process(IInterface context)
        {
            context.PreAwake();
            EditorUtility.SetDirty(context as Object);
        }
#endif

        public interface IInterface
        {
#if UNITY_EDITOR
            void PreAwake();
#endif
        }
    }
}