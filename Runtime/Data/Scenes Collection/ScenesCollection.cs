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
    [ReadOnlySettings]
    [Global(ScriptableManagerScope.Project)]
    [SettingsMenu(Toolbox.Paths.Root + "Scenes Collection")]
    [LoadOrder(Runtime.Defaults.LoadOrder.ScenesCollection)]
    public class ScenesCollection : ScriptableManager<ScenesCollection>
    {
        [SerializeField]
        List<MSceneAsset> list = new List<MSceneAsset>();
        public static List<MSceneAsset> List => Instance.list;

        public static Dictionary<string, MSceneAsset> Dictionary { get; } = new Dictionary<string, MSceneAsset>();

        public static bool TryFind(string id, out MSceneAsset asset) => Dictionary.TryGetValue(id, out asset);

        protected override void OnLoad()
        {
            base.OnLoad();

#if UNITY_EDITOR
            EditorBuildSettings.sceneListChanged += Refresh;
#endif

            Refresh();
        }

        void Refresh()
        {
#if UNITY_EDITOR
            var targets = Extract();

            if (MUtility.CheckElementsInclusion(list, targets, comparer: MSceneAsset.AssetComparer.Instance) == false)
            {
                list = targets;
                Runtime.Save(this);
            }
#endif

            Dictionary.Clear();
            Dictionary.AddAll(List, x => x.ID);
        }

#if UNITY_EDITOR
        protected override void PreProcessBuild()
        {
            base.PreProcessBuild();

            Refresh();
        }

        static List<MSceneAsset> Extract()
        {
            var targets = new List<MSceneAsset>();

            foreach (var entry in EditorBuildSettings.scenes)
            {
                if (entry.enabled == false) continue;

                var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(entry.path);

                var item = new MSceneAsset(asset);

                targets.Add(item);
            }

            return targets;
        }
#endif
    }
}