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
    /// Attribute to apply to fields to make them read only, can specify which play mode to make read only,
    /// NOTE: overrides user defined property drawers, and as such can cause problems, please only use for default drawn types
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class ReadOnlyAttribute : PropertyAttribute
    {
        public ReadOnlyPlayMode Mode { get; }

        public ReadOnlyAttribute() : this(ReadOnlyPlayMode.All) { }
        public ReadOnlyAttribute(ReadOnlyPlayMode mode)
        {
            this.Mode = mode;
        }

        public static bool CheckPlayMode(ReadOnlyPlayMode mode)
        {
            if (mode == ReadOnlyPlayMode.None) return false;
            
            if (mode.HasFlag(ReadOnlyPlayMode.EditMode) && Application.isPlaying == false) return true;
            if (mode.HasFlag(ReadOnlyPlayMode.PlayMode) && Application.isPlaying == true) return true;

            return false;
        }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
        public class Drawer : PropertyDrawer
        {
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
            {
                if(attribute == null)
                {
                    GUI.enabled = false;
                }
                else
                {
                    var mode = (attribute as ReadOnlyAttribute).Mode;

                    var valid = CheckPlayMode(mode);

                    GUI.enabled = !CheckPlayMode(mode);
                }

                EditorGUI.PropertyField(rect, property, label, true);

                GUI.enabled = true;
            }
        }
#endif
    }

    [Flags]
    public enum ReadOnlyPlayMode
    {
        None = 0,
        
        EditMode = 1 << 0,
        PlayMode = 1 << 1,
        
        All = EditMode | PlayMode,
    }
}