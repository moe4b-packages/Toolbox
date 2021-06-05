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
    public class PersistantPropertyDrawer : PropertyDrawer
    {
        protected SerializedProperty Property;

        public SerializedObject SerializedObject => Property.serializedObject;

        protected GUIContent Label;

        protected void Prepare(SerializedProperty reference, GUIContent label)
        {
            if (Property?.propertyPath == reference?.propertyPath) return;

            Property = reference;
            this.Label = label;

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
            return EditorGUI.GetPropertyHeight(Property, Label, true);
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