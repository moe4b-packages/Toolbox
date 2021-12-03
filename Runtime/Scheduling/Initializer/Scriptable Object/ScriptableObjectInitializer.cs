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
    [Manager]
    [ReadOnlySettings]
    [SettingsMenu(Toolbox.Paths.Root + "Scriptable Object Initializer")]
    [LoadOrder(Runtime.Defaults.LoadOrder.ScriptableObjectInitializer)]
    public class ScriptableObjectInitializer : ScriptableManager<ScriptableObjectInitializer>
    {
        [SerializeField]
        List<ScriptableObject> list;
        public List<ScriptableObject> List => list;

        protected override void OnLoad()
        {
            base.OnLoad();

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

            var targets = AssetCollection.FindAll<ScriptableObject>(IsValid);
            static bool IsValid(ScriptableObject target)
            {
                if (target is IInitialize == false)
                    return false;

                if (target is IConditional conditional && conditional.Include == false)
                    return false;

                return true;
            }

            if (MUtility.CheckElementsInclusion(list, targets) == false)
            {
                list = targets;
                Runtime.Save(this);
            }
        }
#endif

        /// <summary>
        /// Implement on ScriptableObject to determine if it's included in initialization process
        /// </summary>
        public interface IConditional
        {
            public bool Include { get; }
        }

        public ScriptableObjectInitializer()
        {
            list = new List<ScriptableObject>();
        }
    }
}