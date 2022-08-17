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
        #region Callbacks
        [InitializeOnLoadMethod]
        static void OnLoad() => ProcessActive();

        class PreProcessBuild : IPreprocessBuildWithReport
        {
            public int callbackOrder => -400;

            public void OnPreprocessBuild(BuildReport report) => PreAwake.InvokeAssets();
        }

        [PostProcessScene]
        static void PostProcessScene()
        {
            var scene = SceneManager.GetActiveScene();

            InvokeScene(scene);
        }
        #endregion

        internal static void ProcessActive()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                InvokeScene(scene);
            }

            InvokeAssets();
        }

        #region Controls
        internal static void InvokeAssets()
        {
            foreach (var asset in AssetCollection.List)
            {
                switch (asset)
                {
                    case GameObject gameObject:
                        using (ComponentQuery.Collection.NonAlloc.InHierarchy<IInterface>(gameObject, out var list))
                        {
                            foreach (var item in list)
                                Invoke(item);
                        }
                        break;

                    case IInterface context:
                        Invoke(context);
                        break;
                }
            }
        }

        static void InvokeScene(Scene scene)
        {
            using (ComponentQuery.Collection.NonAlloc.InScene<IInterface>(scene, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                    Invoke(list[i]);
            }
        }

        internal static void Invoke(IInterface context)
        {
            context.PreAwake();
            MUtility.Unity.SetDirty(context as Object);
        }
        #endregion
#endif

        public interface IInterface
        {
#if UNITY_EDITOR
            void PreAwake();
#endif
        }
    }
}