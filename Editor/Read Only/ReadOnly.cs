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
    public class ReadOnly : PropertyAttribute
    {
#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(ReadOnly))]
        public class Drawer : PersistantPropertyDrawer
        {
            public override float CalculateHeight()
            {
                return EditorGUI.GetPropertyHeight(Property, Label, true);
            }

            public override void Draw(Rect rect)
            {
                GUI.enabled = false;

                EditorGUI.PropertyField(rect, Property, Label, true);

                GUI.enabled = true;
            }
        }
#endif
    }
}