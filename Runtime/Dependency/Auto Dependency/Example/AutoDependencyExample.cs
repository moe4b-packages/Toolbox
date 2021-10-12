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
    [AddComponentMenu(Toolbox.Paths.Example + "Auto Dependency Example")]
    public class AutoDependencyExample : MonoBehaviour
    {
        [AutoDependency(AutoDependency.Parents)]
        public IDependencySample Parent { get; protected set; }

        [AutoDependency(AutoDependency.Self)]
        public IDependencySample Self { get; protected set; }

        [AutoDependency(AutoDependency.Children)]
        public IDependencySample Child { get; protected set; }

        [AutoDependency(AutoDependency.Global)]
        public List<IDependencySample> Global { get; protected set; }

        void Awake()
        {
            AutoDependency.ResolveAll(this);
        }

        void Start()
        {
            Debug.Log($"Parent: {Parent}");
            Debug.Log($"Self: {Self}");
            Debug.Log($"Child: {Child}");

            Debug.Log(Global.ToCollectionString(ToString: x => x.name));
        }
    }
}