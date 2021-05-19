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

namespace MB
{
    public static class Initializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnLoad()
        {
            SceneManager.sceneLoaded += SceneLoadCallback;
        }

        static void SceneLoadCallback(Scene scene, LoadSceneMode mode) => Perform(scene);

        #region Perform
        static void Perform(Scene scene)
        {
            var roots = scene.GetRootGameObjects();

            var targets = new List<IInitialize>();

            for (int i = 0; i < roots.Length; i++)
            {
                var range = roots[i].GetComponentsInChildren<IInitialize>(true);

                targets.AddRange(range);
            }

            Perform(targets);
        }

        public static IList<IInitialize> Perform(UObjectSurrogate surrogate)
        {
            var targets = surrogate.GameObject.GetComponentsInChildren<IInitialize>(true);

            Perform(targets);

            return targets;
        }

        public static void Perform<T>(Func<IEnumerable<T>> function)
            where T : IInitialize
        {
            var collection = function();

            Perform(collection);
        }
        public static void Perform<T>(IEnumerable<T> collection)
            where T : IInitialize
        {
            Configure(collection);
            Init(collection);
        }
        #endregion

        #region Configure
        public static IList<IInitialize> Configure(UObjectSurrogate surrogate)
        {
            var targets = surrogate.GameObject.GetComponentsInChildren<IInitialize>(true);

            Configure(targets);

            return targets;
        }

        public static void Configure<T>(Func<IEnumerable<T>> function)
            where T : IInitialize
        {
            var collection = function();

            Configure(collection);
        }
        public static void Configure<T>(IEnumerable<T> collection)
            where T : IInitialize
        {
            foreach (var item in collection)
                Configure(item);
        }

        public static void Configure(IInitialize instance)
        {
            instance.Configure();
        }
        #endregion

        #region Init
        public static IList<IInitialize> Init(UObjectSurrogate surrogate)
        {
            var targets = surrogate.GameObject.GetComponentsInChildren<IInitialize>(true);

            Init(targets);

            return targets;
        }

        public static void Init<T>(Func<IEnumerable<T>> function)
            where T : IInitialize
        {
            var collection = function();

            Init(collection);
        }
        public static void Init<T>(IEnumerable<T> collection)
            where T : IInitialize
        {
            foreach (var item in collection)
                Init(item);
        }

        public static void Init(IInitialize instance)
        {
            instance.Init();
        }
        #endregion
    }

    public interface IInitialize
    {
        void Configure();

        void Init();
    }
}