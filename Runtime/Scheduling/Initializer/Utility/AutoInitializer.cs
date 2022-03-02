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
    [AddComponentMenu(Initializer.Path + "Auto Initializer")]
	public class AutoInitializer : MonoBehaviour, PreAwake.IInterface
	{
        [ReadOnly]
        [SerializeField]
        Component[] collection;

        public IInitialize[] Targets { get; private set; }

        public void PreAwake()
        {
            var targets = Initializer.Query(this);
            collection = Array.ConvertAll(targets, x => x as Component);
        }

        void Awake()
        {
            Targets = Array.ConvertAll(collection, x => x as IInitialize);

            Initializer.Configure(Targets);
        }
        void Start()
        {
            Initializer.Initialize(Targets);
        }
    }
}