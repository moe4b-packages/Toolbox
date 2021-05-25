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
    /// Attribute to apply to GameObject fields to ensure that only Prefabs (Non Scene Objects) are selected
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class PrefabSelectionAttribute : PropertyAttribute
    {
#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(PrefabSelectionAttribute))]
        public class Drawer : PersistantPropertyDrawer
        {
            protected override float CalculateHeight()
            {
                return EditorGUIUtility.singleLineHeight;
            }

            protected override void Draw(Rect rect)
            {
                PrefabAsset.DrawField(rect, label, property);
            }
        }
#endif
    }
}