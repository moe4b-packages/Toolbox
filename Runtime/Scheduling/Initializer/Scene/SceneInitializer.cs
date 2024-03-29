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
    [AddComponentMenu("")]
	public class SceneInitializer : MonoBehaviour
	{
        [SerializeField]
        Component[] collection;
        public Component[] Collection => collection;

        public IInitialize[] Targets { get; private set; }

        void Awake()
        {
            Targets = Array.ConvertAll(collection, x => x as IInitialize);

            Initializer.Configure(Targets);
        }
        void Start()
        {
            Initializer.Initialize(Targets);
        }

#if UNITY_EDITOR
        bool Refresh()
        {
            var targets = ComponentQuery.Collection.InScene<IInitialize>(gameObject.scene);

            if (targets.Length == 0)
                return false;

            collection = Array.ConvertAll(targets, x => x as Component);
            return true;
        }

        public const int PostProcessOrder = 200;

        [PostProcessScene(PostProcessOrder)]
        static void PostProcess()
        {
            var gameObject = new GameObject("Scene Initializer");
            gameObject.SetActive(false);

            var script = gameObject.AddComponent<SceneInitializer>();

            if (script.Refresh())
                gameObject.SetActive(true);
            else
                DestroyImmediate(gameObject);
        }

        [CustomEditor(typeof(SceneInitializer))]
        public class Inspector : Editor
        {
            public override void OnInspectorGUI()
            {
                GUI.enabled = false;

                EditorGUILayout.HelpBox("This component is auto created at scene build-time, please don't add by hand", MessageType.Warning);

                base.OnInspectorGUI();

                GUI.enabled = true;
            }
        }
#endif
    }
}