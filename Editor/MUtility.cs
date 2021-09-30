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
    }

    public static partial class MUtilityExtensions
    {
        public static void WriteText(this TextAsset asset, string contents)
        {
            var path = AssetDatabase.GetAssetPath(asset);

            File.WriteAllText(path, contents);
        }

        public static IEnumerable<SerializedProperty> IterateChildren(this SerializedObject target)
        {
            var iterator = target.GetIterator();

            iterator.Next(true);

            while (iterator.NextVisible(false))
            {
                if (iterator.name == "m_Script") continue;

                yield return iterator;
            }
        }
    }
}
#endif