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
    /// Surrogate for Unity Objects (gameObject, transform, Component),
    /// just pass one of these whenever a function requires this object
    /// </summary>
    [Serializable]
    public readonly struct UObjectSurrogate
    {
        public readonly GameObject GameObject { get; }
        public readonly Scene Scene => GameObject.scene;

        public readonly Transform Transform => GameObject.transform;

        public UObjectSurrogate(GameObject gameObject)
        {
            this.GameObject = gameObject;
        }

        public static implicit operator UObjectSurrogate(GameObject context) => new UObjectSurrogate(context);
        public static implicit operator UObjectSurrogate(Transform context) => new UObjectSurrogate(context.gameObject);
        public static implicit operator UObjectSurrogate(Component context) => new UObjectSurrogate(context.gameObject);

        public static implicit operator GameObject(UObjectSurrogate context) => context.GameObject;
        public static implicit operator Transform(UObjectSurrogate context) => context.Transform;
    }
}