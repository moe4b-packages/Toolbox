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
        public abstract class GUI : GUIUtility { }
        public abstract class Editor : MEditorUtility { }

        [MenuItem("GameObject/Mark All Dirty")]
        public static void MarkAllDirty()
        {
            foreach (var gameObject in Selection.gameObjects)
            {
                MUtility.UObject.SetDirty(gameObject);

                foreach (var behaviour in gameObject.GetComponentsInChildren<Component>(true))
                {
                    MUtility.UObject.SetDirty(behaviour);
                }
            }
        }
    }

    #region Sub-Classes
    public abstract class GUIUtility
    {
        public static Rect[] SplitHorizontally(Rect rect, float padding, int segments)
        {
            var percentages = new float[segments];

            for (int i = 0; i < percentages.Length; i++)
                percentages[i] = 100 / segments;

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

            rect.yMin += height;

            return area;
        }

        public static Rect SliceHorizontal(ref Rect rect, float width)
        {
            var area = new Rect(rect.x, rect.y, width, rect.height);

            rect.xMin += width;

            return area;
        }
        public static Rect SliceHorizontalPercentage(ref Rect rect, float percentage)
        {
            var width = rect.width * (percentage / 100);

            return SliceHorizontal(ref rect, width);
        }

        public static GUIContent IconContent(string name, string tooltip)
        {
            var icon = EditorGUIUtility.IconContent(name);
            return new GUIContent(icon.image, tooltip);
        }
    }
    public static class GUIExtensions
    {
        public static void AddItem(this GenericMenu menu, string text, bool on, GenericMenu.MenuFunction function)
        {
            var content = new GUIContent(text);

            menu.AddItem(content, on, function);
        }
        public static void AddItem(this GenericMenu menu, string text, bool on, GenericMenu.MenuFunction2 function, object data)
        {
            var content = new GUIContent(text);

            menu.AddItem(content, on, function, data);
        }
    }

    public static class IOExtensions
    {
        public static void WriteText(this TextAsset asset, string contents)
        {
            var path = AssetDatabase.GetAssetPath(asset);

            File.WriteAllText(path, contents);

            AssetDatabase.ImportAsset(path);
        }
    }

    public static class SerializationExtensions
    {
        public static IEnumerable<SerializedProperty> IterateChildren(this SerializedObject target)
        {
            var iterator = target.GetIterator();

            iterator.Next(true);

            while (iterator.NextVisible(false))
            {
                if (iterator.name == "m_Script") continue;

                yield return iterator.Copy();
            }
        }

        public static string GetEnumValueName(this SerializedProperty property)
        {
            if (property.enumDisplayNames.ValidateBounds(property.enumValueIndex))
                return property.enumDisplayNames[property.enumValueIndex];

            return $"Undefined: {property.intValue}";
        }

        public static GUIContent GetDisplayContent(this SerializedProperty property)
        {
            return new GUIContent(property.displayName, property.tooltip);
        }

        public static void LateModifyProperty(this SerializedProperty property, Action<SerializedProperty> action)
        {
            property.serializedObject.Update();
            action(property);
            property.serializedObject.ApplyModifiedProperties();
        }

        public static bool IsEditingMultipleObjects(this SerializedProperty property)
        {
            return property.serializedObject.targetObjects.Length > 1;
        }
    }

    public abstract class MEditorUtility
    {
        public static class ProjectIncludeExtensions
        {
            static PropertyInfo PropertyInfo { get; }

            public static HashSet<string> Retrieve()
            {
                var text = (PropertyInfo.GetValue(default) as string).ToLower();
                var set = text.Split(';', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
                return set;
            }
            public static void Set(HashSet<string> collection)
            {
                var text = string.Join(';', collection).ToLower();

                PropertyInfo.SetValue(default, text);
            }

            public static Handle Modify(out HashSet<string> collection)
            {
                collection = Retrieve();
                return new Handle(collection);
            }

            public struct Handle : IDisposable
            {
                public HashSet<string> Collection { get; }

                public void Dispose()
                {
                    Set(Collection);
                }

                public Handle(HashSet<string> collection)
                {
                    this.Collection = collection;
                }
            }

            static ProjectIncludeExtensions()
            {
                var flags = BindingFlags.Static | BindingFlags.NonPublic;
                PropertyInfo = typeof(EditorSettings).GetProperty("Internal_ProjectGenerationUserExtensions", flags);
            }
        }
    }
    #endregion
}
#endif