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
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditorInternal;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using System.Reflection;

namespace MB
{
    [CreateAssetMenu(menuName = Toolbox.Path + "Scriptable Object Initializer")]
	public class ScriptableObjectInitializer : GlobalScriptableObject<ScriptableObjectInitializer>, IScriptableObjectBuildPreProcess
	{
        [SerializeField]
        protected List<ScriptableObject> list;
        public IReadOnlyList<ScriptableObject> List { get { return list; } }

        protected override void Load()
        {
            base.Load();

#if UNITY_EDITOR
            Refresh();
#endif

            Perform();
        }

        protected virtual void Perform()
        {
            var interfaces = new IInitialize[list.Count];

            for (int i = 0; i < list.Count; i++)
                interfaces[i] = list[i] as IInitialize;

            Initializer.Perform(interfaces);
        }

#if UNITY_EDITOR
        public virtual void Refresh()
        {
            list.Clear();

            var guids = AssetDatabase.FindAssets("t:ScriptableObject");

            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);

                var instance = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                if (instance is IInitialize) list.Add(instance);
            }

            EditorUtility.SetDirty(this);
        }

        public void PreProcessBuild() => Refresh();
#endif

        public ScriptableObjectInitializer()
        {
            list = new List<ScriptableObject>();
        }

#if UNITY_EDITOR
        class BuildProcessor : IPreprocessBuildWithReport
        {
            public ScriptableObjectInitializer Instance => ScriptableObjectInitializer.Instance;

            public int callbackOrder => 0;

            public void OnPreprocessBuild(BuildReport report) => Instance.Refresh();
            public void OnPreprocessBuild(BuildTarget target, string path) => Instance.Refresh();
        }
#endif
    }
}