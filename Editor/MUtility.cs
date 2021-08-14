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

using System.Reflection;

namespace MB
{
    /// <summary>
    /// A collection of random utility functions
    /// </summary>
    public static partial class MUtility
    {
        public static class GUICoordinates
        {
            public static Rect[] SplitHorizontally(Rect rect, float padding, int cuts)
            {
                var percentages = new float[cuts];

                for (int i = 0; i < percentages.Length; i++)
                    percentages[i] = 100 / cuts;

                return SplitHorizontally(rect, padding, percentages);
            }

            public static Rect[] SplitHorizontally(Rect rect, float padding, params float[] cuts)
            {
                padding /= 2f;

                var areas = new Rect[cuts.Length];

                var width = rect.width;
                var x = rect.x;

                for (int i = 0; i < areas.Length; i++)
                {
                    var span = (width * cuts[i] / 100f);

                    areas[i] = new Rect(x + padding, rect.y, span - padding, rect.height);

                    x += span;
                }

                return areas;
            }

            public static Rect SliceLine(ref Rect rect) => SliceLine(ref rect, EditorGUIUtility.singleLineHeight);
            public static Rect SliceLine(ref Rect rect, float height)
            {
                var area = new Rect(rect.x, rect.y, rect.width, height);

                rect.y += area.height;
                rect.height -= area.height;

                return area;
            }

            public static void ClearIndent()
            {
                Indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
            }
            static int Indent;
            public static void RestoreIndent()
            {
                EditorGUI.indentLevel = Indent;
            }
        }

        public static class SerializedPropertyType
        {
            public static Dictionary<string, Type> Cache { get; private set; }

            public static Type Retrieve(SerializedProperty property)
            {
                var id = FormatID(property);

                if (Cache.TryGetValue(id, out var type))
                    return type;

                var path = property.propertyPath.Replace(".Array.data", "");
                var segments = path.Split('.');

                var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

                type = property.serializedObject.targetObject.GetType();

                for (int i = 0; i < segments.Length; i++)
                {
                    ParsePathSegmenet(segments[i], out var segment, out var isArray);

                    type = type.GetField(segment, flags).FieldType;

                    if (isArray) type = type.GetElementType();
                }

                Cache[id] = type;

                return type;
            }

            static string FormatID(SerializedProperty property)
            {
                var type = property.serializedObject.targetObject.GetType();

                return $"{type.FullName}.{property.propertyPath}";
            }

            static void ParsePathSegmenet(string text, out string segmenet, out bool isArray)
            {
                var index = text.IndexOf("[");

                if (index < 0)
                {
                    segmenet = text;
                    isArray = false;
                }
                else
                {
                    segmenet = text.Remove(index);
                    isArray = true;
                }
            }

            static SerializedPropertyType()
            {
                Cache = new Dictionary<string, Type>();
            }
        }
    }

    public static partial class MUtilityExtensions
    {
        public static void WriteText(this TextAsset asset, string contents)
        {
            var path = AssetDatabase.GetAssetPath(asset);

            File.WriteAllText(path, contents);
        }
    }
}
#endif