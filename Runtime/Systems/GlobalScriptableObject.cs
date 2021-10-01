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

        //Static Utility

        public static GlobalScriptableObject Retrieve(Type type)
        {
            var name = MUtility.PrettifyName(type.Name);

            IList<Object> assets = Resources.LoadAll("", type); ;

            if (assets.Count == 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"No {type.Name} Instance Found, Creating Asset");
                return CreateAsset(type, name);
#endif

                throw new Exception($"No {name} Instance Found, Ignoring System Load");
            }
            else
            {
#if UNITY_EDITOR
                if (assets.Count > 1)
                {
                    Debug.LogWarning($"Multiple Instances of {name} Found in Project, Deleting Unecessary Instances");
                    DeleteAssets(assets, assets[0]);
                }
#endif

                return assets[0] as GlobalScriptableObject;
            }
        }

#if UNITY_EDITOR
        public static GlobalScriptableObject CreateAsset(Type type, string name)
        {
            var asset = CreateInstance(type) as GlobalScriptableObject;

            var directory = new DirectoryInfo($"Assets/{Toolbox.Name}/Resources");
            if (directory.Exists == false) directory.Create();

            AssetDatabase.CreateAsset(asset, $"{directory}/{name}.asset");

            return asset;
        }

        public static void DeleteAssets<T>(IList<T> list, T exception)
            where T : Object
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

    public class GlobalScriptableObject<T> : GlobalScriptableObject
        where T : GlobalScriptableObject<T>
    {
        public static T Instance { get; protected set; }

        public override void OnEnable()
        {
            base.OnEnable();

            Instance = this as T;

            Load();
        }
    }
}