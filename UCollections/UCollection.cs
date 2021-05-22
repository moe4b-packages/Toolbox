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
        public abstract class BaseDrawer : PropertyDrawer
        {
            protected SerializedProperty property;

            public bool IsExpanded
            {
                get => property.isExpanded;
                set => property.isExpanded = value;
            }

            protected SerializedProperty list;
            protected abstract SerializedProperty GetList();

            protected GUIContent label;
            protected virtual void SetLabel(GUIContent content)
            {
                var text = content.text.Insert(0, " ");

                label = new GUIContent(text, content.image, content.tooltip);
            }

            protected ReorderableList gui;
            protected ReorderableList.Defaults defaults;

            public const float ElementHeightPadding = 6f;
            public const float ElementFoldoutPadding = 15f;

            public float ListPadding = 5f;

            public static float SingleLineHeight => EditorGUIUtility.singleLineHeight;

            protected virtual void Set(SerializedProperty reference)
            {
                if (property?.propertyPath == reference.propertyPath) return;

                property = reference;

                Init();
            }

            protected virtual void Init()
            {
                list = GetList();

                defaults = new ReorderableList.Defaults();

                gui = new ReorderableList(property.serializedObject, list, true, true, true, true);

                gui.drawHeaderCallback = DrawHeader;
                gui.elementHeightCallback = GetElementHeight;
                gui.drawElementCallback = DrawElement;
            }

            #region Height
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                Set(property);

                var height = ListPadding * 2;

                return CalculateHeight(height);
            }

            protected virtual float CalculateHeight(float height)
            {
                height += IsExpanded ? gui.GetHeight() : gui.headerHeight;

                return height;
            }

            protected virtual float GetElementHeight(int index)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(index);

                var height = EditorGUI.GetPropertyHeight(element);

                var max = Math.Max(height, SingleLineHeight);

                return max + ElementHeightPadding;
            }
            #endregion

            #region Draw
            public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
            {
                Set(property);

                SetLabel(label);

                rect = EditorGUI.IndentedRect(rect);

                rect.y += ListPadding;
                rect.height -= ListPadding + ListPadding;

                Draw(rect);
            }

            protected virtual void Draw(Rect rect)
            {
                EditorGUI.indentLevel = 0;

                if (IsExpanded)
                    DrawList(rect);
                else
                    DrawHeader(rect, true);
            }

            protected virtual void DrawList(Rect rect)
            {
                gui.DoList(rect);
            }

            protected virtual void DrawHeader(Rect rect) => DrawHeader(rect, false);
            protected virtual void DrawHeader(Rect rect, bool full)
            {
                if (full)
                {
                    defaults.DrawHeaderBackground(rect);

                    rect.x += 6;
                    rect.y += 0;
                }

                var indent = EditorGUI.indentLevel;

                EditorGUI.indentLevel = 0;

                rect.x += 10f;

                IsExpanded = EditorGUI.Foldout(rect, IsExpanded, label, true);

                EditorGUI.indentLevel = indent;
            }

            protected virtual void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
            {
                rect.height -= ElementHeightPadding;
                rect.y += ElementHeightPadding / 2;

                var element = list.GetArrayElementAtIndex(index);

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