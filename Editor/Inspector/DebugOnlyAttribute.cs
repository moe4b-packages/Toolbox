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
    /// Attribute to apply to fields to make them viewable only in inspector debug mode
    /// NOTE: overrides user defined property drawers, and as such can cause problems, please only use for default drawn types
    /// </summary>
    public class DebugOnlyAttribute : PropertyAttribute
    {
#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(DebugOnlyAttribute))]
        public class Drawer : PropertyDrawer
        {
            static bool Visibile => ActiveEditorTracker.sharedTracker.inspectorMode != InspectorMode.Normal;

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                if (Visibile)
                    return EditorGUI.GetPropertyHeight(property, label, true);

                return 0f;
            }

            public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
            {
                GUI.enabled = false;

                if (Visibile)
                    EditorGUI.PropertyField(rect, property, label, true);

                GUI.enabled = true;
            }
        }
#endif
    }
}