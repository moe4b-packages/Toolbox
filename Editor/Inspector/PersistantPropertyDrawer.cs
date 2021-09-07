#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

using UnityEditor;
using UnityEditorInternal;

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace MB
{
    public class PersistantPropertyDrawer : PropertyDrawer
    {
        protected SerializedProperty property;

        public SerializedObject SerializedObject => property.serializedObject;

        protected GUIContent label;

        protected void Prepare(SerializedProperty reference, GUIContent label)
        {
            if (property?.propertyPath == reference?.propertyPath) return;

            property = reference;
            this.label = label;

            Init();
        }

        protected virtual void Init()
        {

        }

        #region Height
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            Prepare(property, label);

            return CalculateHeight();
        }

        public virtual float CalculateHeight()
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
        #endregion

        #region GUI
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            Prepare(property, label);

            Draw(rect);
        }

        public virtual void Draw(Rect rect)
        {

        }
        #endregion
    }
}
#endif