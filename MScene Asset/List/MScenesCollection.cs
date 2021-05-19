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
	[CreateAssetMenu(menuName = Toolbox.Path + "MScenes Collection")]
	public class MScenesCollection : GlobalScriptableObject<MScenesCollection>, IInitialize, IScriptableObjectBuildPreProcess
	{
        [SerializeField]
        List<MSceneAsset> list = default;
        public List<MSceneAsset> Collection => list;

        public Dictionary<string, MSceneAsset> Dictionary { get; private set; }

        public static bool TryFind(string id, out MSceneAsset asset) => Instance.Dictionary.TryGetValue(id, out asset);

        public void Configure()
        {
#if UNITY_EDITOR
            Refresh();
#endif

            Dictionary = list.ToDictionary(x => x.ID);
        }

        public void Init() { }

#if UNITY_EDITOR
        void Refresh()
        {
            list.Clear();

            foreach (var entry in EditorBuildSettings.scenes)
            {
                if (entry.enabled == false) continue;

                var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(entry.path);

                var item = new MSceneAsset(asset);

                list.Add(item);
            }

            EditorUtility.SetDirty(this);
        }

        public void PreProcessBuild() => Refresh();
#endif

        public MScenesCollection()
        {
            list = new List<MSceneAsset>();
        }
    }
}