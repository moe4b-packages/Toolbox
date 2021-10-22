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
    public class ScenesCollection : ScriptableManager<ScenesCollection>
    {
        [SerializeField]
        List<MSceneAsset> list = new List<MSceneAsset>();
        public static List<MSceneAsset> List => Instance.list;

        public static Dictionary<string, MSceneAsset> Dictionary { get; } = new Dictionary<string, MSceneAsset>();
        static void UpdateDictionary()
        {
            Dictionary.Clear();

            Dictionary.AddAll(List, x => x.ID);
        }

        public static bool TryFind(string id, out MSceneAsset asset) => Dictionary.TryGetValue(id, out asset);

        protected override void OnLoad()
        {
            base.OnLoad();

#if UNITY_EDITOR
            Refresh();

            EditorBuildSettings.sceneListChanged += Refresh;
#endif

            if (Application.isEditor == false) UpdateDictionary();
        }

#if UNITY_EDITOR
        void Refresh()
        {
            var targets = Extract();

            if (MUtility.CheckElementsInclusion(list, targets, comparer: MSceneAsset.AssetComparer.Instance) == false)
            {
                list = targets;
                UpdateDictionary();
                ScriptableManagerRuntime.Save(this);
            }
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