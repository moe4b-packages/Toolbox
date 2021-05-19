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
	public class Core : MonoBehaviour
	{
        public class Behaviour : MonoBehaviour, IBehaviour<Core>
        {
            public virtual void Configure()
            {

            }

            public virtual void Init()
            {

            }
        }

        public class Module : Behaviour, IModule<Core>
        {
            public Core Core { get; protected set; }

            public void Set(Core reference)
            {
                Core = reference;
            }
        }
    }
}