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

using UnityEditor.Callbacks;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace MB
{
    /// <summary>
    /// A System for configuring (Awake) & initiating (Start) classes independently of Unity's reflection based method,
    /// usefull for preparing UI because Awake & Start methods don't get called on inactive objects and UI is often inactive on play
    /// </summary>
    public static class Initializer
    {
        public const string Path = Toolbox.Paths.Box + "Initializer/";
        
        #region Perform
        public static void Perform(UObjectSurrogate surrogate)
        {
            using (ComponentQuery.Collection.NonAlloc.InHierarchy<IInitialize>(surrogate, out var targets))
            {
                Perform(targets);
            }
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
            Initialize(collection);
        }
        #endregion

        #region Configure
        public static void Configure(UObjectSurrogate surrogate)
        {
            using (ComponentQuery.Collection.NonAlloc.InHierarchy<IInitialize>(surrogate, out var targets))
            {
                Configure(targets);
            }
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
        public static void Init(UObjectSurrogate surrogate)
        {
            using (ComponentQuery.Collection.NonAlloc.InHierarchy<IInitialize>(surrogate, out var targets))
            {
                Initialize(targets);
            }
        }

        public static void Initialize<T>(Func<IEnumerable<T>> function)
            where T : IInitialize
        {
            var collection = function();

            Initialize(collection);
        }
        public static void Initialize<T>(IEnumerable<T> collection)
            where T : IInitialize
        {
            foreach (var item in collection)
                Initialize(item);
        }

        public static void Initialize(IInitialize instance)
        {
            instance.Initialize();
        }
        #endregion

        public static IInitialize[] Query(UObjectSurrogate surrogate)
        {
            return ComponentQuery.Collection.InHierarchy<IInitialize>(surrogate);
        }
    }

    public interface IInitialize
    {
        void Configure();

        void Initialize();
    }
}