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
	public class AutoInitializer : MonoBehaviour
	{
        IList<IInitialize> list;

        void Awake()
        {
            list = Initializer.Configure(this);
        }

        void Start()
        {
            Initializer.Init(list);
        }
    }
}