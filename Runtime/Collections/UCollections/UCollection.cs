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
    /// A Collection of serializable collections
    /// </summary>
    public abstract class UCollection : IUCollection
    {
        public abstract int Count { get; }

#if UNITY_EDITOR
        public abstract class BaseDrawer : PersistantPropertyDrawer
        {
            protected SerializedProperty List;
            protected abstract SerializedProperty GetList();

            protected ImprovedReorderableList UI;

            public float ListPadding = 5f;

            public static float SingleLineHeight => EditorGUIUtility.singleLineHeight;

            public const float ElementHeightPadding = 6f;
            public const float ElementFoldoutPadding = 15f;

            protected override void Init()
            {
                base.Init();

                List = GetList();

                UI = new ImprovedReorderableList(List);

                UI.TitleText = Property.displayName;

                UI.GetElementHeight = GetElementHeight;
                UI.DrawElement = DrawElement;
            }

            #region Height
            public override float CalculateHeight()
            {
                var height = ListPadding * 2;

                return AppendHeight(height);
            }

            protected virtual float AppendHeight(float height)
            {
                height += UI.CalculateHeight();

                return height;
            }

            protected virtual float GetElementHeight(int index)
            {
                SerializedProperty element = List.GetArrayElementAtIndex(index);

                var height = EditorGUI.GetPropertyHeight(element);

                var max = Math.Max(height, SingleLineHeight);

                return max + ElementHeightPadding;
            }
            #endregion

            #region Draw
            public override void Draw(Rect rect)
            {
                rect = EditorGUI.IndentedRect(rect);
                EditorGUI.indentLevel = 0;

                rect.y += ListPadding;
                rect.height -= ListPadding + ListPadding;

                UI.Draw(rect);
            }

            protected virtual void DrawElement(Rect rect, int index)
            {
                rect.height -= ElementHeightPadding;
                rect.y += ElementHeightPadding / 2;

                var element = List.GetArrayElementAtIndex(index);

                DrawField(rect, element);
            }

            protected virtual void DrawField(Rect rect, SerializedProperty property)
            {
                if (IsInline(property) == false)
                {
                    rect.x += ElementFoldoutPadding;
                    rect.width -= ElementFoldoutPadding;
                }

                EditorGUI.PropertyField(rect, property, true);
            }
            #endregion

            #region Static Utility
            public static bool IsInline(SerializedProperty property)
            {
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Generic:
                        return property.hasVisibleChildren == false;
                }

                return true;
            }

            public static Rect[] Split(Rect source, params float[] cuts)
            {
                var rects = new Rect[cuts.Length];

                var x = 0f;

                for (int i = 0; i < cuts.Length; i++)
                {
                    rects[i] = new Rect(source);

                    rects[i].x += x;
                    rects[i].width *= cuts[i] / 100;

                    x += rects[i].width;
                }

                return rects;
            }

            public static IEnumerable<SerializedProperty> IterateChildren(SerializedProperty property)
            {
                var path = property.propertyPath;

                property.Next(true);

                while (true)
                {
                    yield return property;

                    if (property.NextVisible(false) == false) break;
                    if (property.propertyPath.StartsWith(path) == false) break;
                }
            }

            public static float GetChildrenSingleHeight(SerializedProperty property, float spacing)
            {
                if (IsInline(property)) return SingleLineHeight;

                var height = 0f;

                foreach (var child in IterateChildren(property))
                    height += SingleLineHeight + spacing;

                return height;
            }

            public static void DeleteArrayRange(SerializedProperty array, int difference)
            {
                for (int i = 0; i < difference; i++)
                {
                    var index = array.arraySize - 1;

                    ForceDeleteArrayElement(array, index);
                }
            }

            public static void ForceDeleteArrayElement(SerializedProperty array, int index)
            {
                var property = array.GetArrayElementAtIndex(index);

                if (property.propertyType == SerializedPropertyType.ObjectReference)
                    property.objectReferenceValue = null;

                array.DeleteArrayElementAtIndex(index);
            }

            public static GUIContent GetIconContent(string id, string tooltip)
            {
                var icon = EditorGUIUtility.IconContent(id);

                return new GUIContent(icon.image, tooltip);
            }
            #endregion
        }
#endif
    }

    public interface IUCollection
    {
        int Count { get; }
    }
}