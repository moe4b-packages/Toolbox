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

namespace MB
{
    /// <summary>
    /// A base class for creating a singelton ScriptableObject that will be loaded dynamically from Resources
    /// </summary>
    public class GlobalScriptableObject : ScriptableObject
    {
        [InitializeOnAllLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Initiate()
        {
            var list = TypeQuery.FindAll(Predicate);

            var flags = BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

            for (int i = 0; i < list.Count; i++)
            {
                var method = list[i].GetMethod("OnLoad", flags);
                method.Invoke(null, null);
            }
        }

        static bool Predicate(Type type)
        {
            if (type == typeof(GlobalScriptableObject))
                return false;

            if (type.IsGenericType)
                return false;

            if (typeof(GlobalScriptableObject).IsAssignableFrom(type) == false)
                return false;

            return true;
        }
    }

    public class GlobalScriptableObject<T> : GlobalScriptableObject
        where T : GlobalScriptableObject<T>
    {
        public static T Instance { get; protected set; }

        public static string Name { get; } = MUtility.PrettifyName(typeof(T).Name);

        public static void OnLoad()
        {
            var assets = RetrieveAll();

            if (assets.Count == 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"No {Name} Instance Found, Creating Asset");
                Instance = CreateAsset();
#else
                Debug.LogWarning($"No {Name} Instance Found, Ignoring System Load");
                return;
#endif
            }
            else
            {
                Instance = assets[0];

#if UNITY_EDITOR
                if (assets.Count > 1)
                {
                    Debug.LogWarning($"Multiple Instances of {Name} Found in Project, Deleting Unecessary Instances");
                    DeleteAssets(assets, Instance);
                }
#endif
            }

            Instance.Load();
        }

        protected virtual void Load()
        {

        }

        public static IList<T> RetrieveAll() => Resources.LoadAll<T>("");

#if UNITY_EDITOR
        static T CreateAsset()
        {
            var asset = CreateInstance<T>();

            var directory = new DirectoryInfo($"Assets/{Toolbox.Name}/Resources");
            if (directory.Exists == false) directory.Create();

            AssetDatabase.CreateAsset(asset, $"{directory}/{Name}.asset");

            return asset;
        }

        static void DeleteAssets(IList<T> list, T exception)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == exception) continue;

                var path = AssetDatabase.GetAssetPath(list[i]);

                AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.SaveAssets();
        }
#endif
    }
}