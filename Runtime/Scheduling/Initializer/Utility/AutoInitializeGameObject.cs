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
    /// <summary>
    /// Component that allows Auto Initializing a GameObject, To be Placed on Prefabs
    /// </summary>
    [AddComponentMenu(Initializer.Path + "Auto Initialize GameObject")]
    public class AutoInitializeGameObject : MonoBehaviour, PreAwake.IInterface
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