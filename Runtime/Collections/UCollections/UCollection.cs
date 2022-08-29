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
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(UDictionary<,>), true)]
    [CustomPropertyDrawer(typeof(UHashSet<>), true)]
    public class UCollectionDrawer : PropertyDrawer
    {
        public static void Initiate(SerializedProperty property, out SerializedProperty list)
        {
            list = property.FindPropertyRelative("list");
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            Initiate(property, out var list);

            return EditorGUI.GetPropertyHeight(list, true);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            Initiate(property, out var list);

            EditorGUI.PropertyField(rect, list, label, true);
        }
    }

    public abstract class UCollectionEntryDrawer : PropertyDrawer
    {
        public const float Padding = 3f;

        static GUIContent ErrorContent = MUtility.GUI.IconContent("console.erroricon.sml", "Invalid Element");

        public static void RetrieveIsValid(SerializedProperty property, out SerializedProperty isValid)
        {
            isValid = property.FindPropertyRelative(MUtility.Type.FormatPropertyBackingFieldName("IsValid"));
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            RetrieveIsValid(property, out var isValid);

            rect.y += Padding / 2f;
            rect.height -= Padding;

            //Draw Error
            {
                var area = new Rect();

                if (isValid.boolValue == false)
                {
                    var height = Math.Min(EditorGUIUtility.singleLineHeight, rect.height);

                    area = MUtility.GUI.SliceHorizontal(ref rect, height);
                    area.height = height;

                    area.x -= 5f;
                }

                DrawError(area);
            }

            DrawContent(rect, property, label, isValid.boolValue);
        }

        public virtual void DrawError(Rect rect)
        {
            EditorGUI.LabelField(rect, ErrorContent);
        }

        protected abstract void DrawContent(Rect rect, SerializedProperty property, GUIContent label, bool isValid);

        public static void DrawShortField(Rect rect, SerializedProperty property)
        {
            EditorGUIUtility.labelWidth /= 1.5f;

            if (IsNestedProperty(property))
            {
                rect.xMin += 10f;

                EditorGUI.PropertyField(rect, property, true);
            }
            else
            {
                EditorGUI.PropertyField(rect, property, GUIContent.none, true);
            }
        }

        public static bool IsNestedProperty(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.Generic)
                return false;

            if (property.hasVisibleChildren == false)
                return false;

            if (property.type == "UnityEvent")
                return false;

            return true;
        }
    }
#endif
}