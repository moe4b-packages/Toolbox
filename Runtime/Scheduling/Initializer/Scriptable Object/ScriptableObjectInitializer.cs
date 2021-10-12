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
    [ReadOnlySettings]
    [Global(ScriptableManagerScope.Project)]
    [SettingsMenu(Toolbox.Paths.Root + "Scriptable Object Initializer")]
    public class ScriptableObjectInitializer : ScriptableManager<ScriptableObjectInitializer>
    {
        [SerializeField]
        List<ScriptableObject> list;
        public List<ScriptableObject> List => list;

        protected internal override bool IncludeInBuild => list.Count > 0;

        protected override void Load()
        {
            base.Load();

            #if UNITY_EDITOR
            Refresh();

            AssetCollection.OnRefresh += Refresh;
            #endif

            Perform();
        }

        private void Perform()
        {
            var interfaces = new IInitialize[list.Count];

            for (var i = 0; i < list.Count; i++)
                interfaces[i] = list[i] as IInitialize;

            Initializer.Perform(interfaces);
        }

#if UNITY_EDITOR
        public void Refresh()
        {
            if (this == null) return;

            var targets = AssetCollection.Query<ScriptableObject>(x => x is IInitialize);

            if (MUtility.CheckElementsInclusion(list, targets) == false)
            {
                list = targets;
                EditorUtility.SetDirty(this);
            }
        }
#endif

        public ScriptableObjectInitializer()
        {
            list = new List<ScriptableObject>();
        }
    }
}