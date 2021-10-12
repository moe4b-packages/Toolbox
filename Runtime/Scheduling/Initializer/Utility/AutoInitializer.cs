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
	public class AutoInitializer : MonoBehaviour
	{
        IInitialize[] collection;

        void Awake()
        {
            collection = Initializer.Query(this);

            Initializer.Configure(collection);
        }

        void Start()
        {
            Initializer.Init(collection);
        }
    }
}