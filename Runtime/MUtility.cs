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

using UnityEngine.LowLevel;

using System.Text;
using System.Reflection;
using System.Collections.Concurrent;

using System.Threading;
using System.Threading.Tasks;

namespace MB
{
    /// <summary>
    /// A collection of random utility functions
    /// </summary>
    public static partial class MUtility
    {
        public static RuntimePlatform CheckPlatform()
        {
#if UNITY_EDITOR
            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.StandaloneOSX:
                    return RuntimePlatform.OSXPlayer;

                case BuildTarget.StandaloneWindows:
                    return RuntimePlatform.WindowsPlayer;

                case BuildTarget.iOS:
                    return RuntimePlatform.IPhonePlayer;

                case BuildTarget.Android:
                    return RuntimePlatform.Android;

                case BuildTarget.StandaloneWindows64:
                    return RuntimePlatform.WindowsPlayer;

                case BuildTarget.WebGL:
                    return RuntimePlatform.WebGLPlayer;

                case BuildTarget.WSAPlayer:
                    return RuntimePlatform.WSAPlayerX64;

                case BuildTarget.StandaloneLinux64:
                    return RuntimePlatform.LinuxPlayer;

                case BuildTarget.PS4:
                    return RuntimePlatform.PS4;

                case BuildTarget.XboxOne:
                    return RuntimePlatform.XboxOne;

                case BuildTarget.tvOS:
                    return RuntimePlatform.tvOS;

                case BuildTarget.Switch:
                    return RuntimePlatform.Switch;

                case BuildTarget.Lumin:
                    return RuntimePlatform.Lumin;

                case BuildTarget.Stadia:
                    return RuntimePlatform.Stadia;
            }
#endif

            return Application.platform;
        }

        public static void RegisterPlayerLoop<TType>(PlayerLoopSystem.UpdateFunction callback)
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();

            for (int i = 0; i < loop.subSystemList.Length; ++i)
                if (loop.subSystemList[i].type == typeof(TType))
                    loop.subSystemList[i].updateDelegate += callback;

            PlayerLoop.SetPlayerLoop(loop);
        }

        public static string PrettifyName<T>(T value)
        {
            var text = value.ToString();

            var builder = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                var current = text[i];

                var next = ValidateIndexBounds(text.Length, i + 1) ? text[i + 1] : default;
                var previous = ValidateIndexBounds(text.Length, i - 1) ? text[i - 1] : default;

                if (char.IsUpper(current))
                {
                    if (char.IsLower(previous))
                        builder.Append(' ');
                    else if (char.IsUpper(previous) && char.IsLower(next))
                        builder.Append(' ');
                }

                if (char.IsNumber(current) && !char.IsNumber(previous))
                    builder.Append(' ');

                if (!char.IsNumber(current) && char.IsNumber(previous))
                    builder.Append(' ');

                if (text[i] == '_') current = ' ';

                builder.Append(current);
            }

            return builder.ToString();
        }

        #region Calculate Bounds
        public static Bounds CalculateRendererBounds(UObjectSurrogate surrogate) => CalculateRendererBounds(surrogate, true);
        public static Bounds CalculateRendererBounds(UObjectSurrogate surrogate, bool includeInactive)
        {
            var targets = surrogate.GameObject.GetComponentsInChildren<Renderer>(includeInactive);

            Bounds Extract(Renderer renderer) => renderer.bounds;

            return CalculateBounds(surrogate, targets, Extract);
        }

        public static Bounds CalculateColliderBounds(UObjectSurrogate surrogate) => CalculateColliderBounds(surrogate, true);
        public static Bounds CalculateColliderBounds(UObjectSurrogate surrogate, bool includeInactive)
        {
            var targets = surrogate.GameObject.GetComponentsInChildren<Collider>(includeInactive);

            Bounds Extract(Collider renderer) => renderer.bounds;

            return CalculateBounds(surrogate, targets, Extract);
        }

        public static Bounds CalculateCollider2DBounds(UObjectSurrogate surrogate) => CalculateColliderBounds(surrogate, true);
        public static Bounds CalculateCollider2DBounds(UObjectSurrogate surrogate, bool includeInactive)
        {
            var targets = surrogate.GameObject.GetComponentsInChildren<Collider2D>(includeInactive);

            Bounds Extract(Collider2D renderer) => renderer.bounds;

            return CalculateBounds(surrogate, targets, Extract);
        }

        public static Bounds CalculateBounds<T>(Transform transform, IList<T> list, Func<T, Bounds> extract)
        {
            if (list.Count == 0)
                return new Bounds(transform.position, Vector3.zero);

            var bound = extract(list[0]);

            for (int i = 1; i < list.Count; i++)
            {
                var context = extract(list[i]);

                bound.Encapsulate(context);
            }

            return bound;
        }
        #endregion

        #region Layer
        public static void SetLayer(UObjectSurrogate surrogate, string name)
        {
            var index = LayerMask.NameToLayer(name);

            SetLayer(surrogate, index);
        }

        public static void SetLayer(UObjectSurrogate surrogate, int index)
        {
            surrogate.GameObject.layer = index;

            for (int i = 0; i < surrogate.Transform.childCount; i++)
                SetLayer(surrogate.Transform.GetChild(i), index);
        }
        #endregion

        #region Collections
        /// <summary>
        /// Returns element at index, or returns null if index out of bounds
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static T SafeIndexer<T>(this IList<T> collection, int index)
            where T : class
        {
            if (ValidateCollectionBounds(collection, index) == false) return null;

            return collection[index];
        }

        public static bool TryGet<T>(this IList<T> collection, int index, out T value)
        {
            if (collection.ValidateCollectionBounds(index) == false)
            {
                value = default;
                return false;
            }

            value = collection[index];
            return true;
        }

        public static bool ValidateCollectionBounds<T>(this ICollection<T> collection, int index) => ValidateIndexBounds(collection.Count, index);
        public static bool ValidateIndexBounds(int length, int index)
        {
            if (index < 0 || index + 1 > length) return false;

            return true;
        }

        /// <summary>
        /// Checks that elements in the original collect exist in the latest collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="original"></param>
        /// <param name="latest"></param>
        /// <returns></returns>
        public static bool CheckElementsInclusion<T>(IEnumerable<T> original, IEnumerable<T> latest, IEqualityComparer<T> comparer = null)
        {
            if (original == null && original == null) return true;

            if (original == null && original != null) return false;
            if (original != null && original == null) return false;

            if (comparer == null) comparer = EqualityComparer<T>.Default;

            var hashset = new HashSet<T>(original, comparer);

            foreach (var item in latest)
                if (hashset.Remove(item) == false)
                    return false;

            if (hashset.Count > 0) return false;

            return true;
        }

        public static T GetRandomElement<T>(IList<T> list)
        {
            var index = Random.Range(0, list.Count);

            return list[index];
        }
        #endregion

        #region Audio
        public static float LinearToDecibel(float linear)
        {
            if (linear == 0)
                return -144.0f;
            else
                return Mathf.Log10(linear) * 20.0f;
        }

        public static float DecibelToLinear(float dB)
        {
            return Mathf.Pow(10.0f, dB / 20.0f);
        }
        #endregion

        public static string GetHierarchyPath(UObjectSurrogate surrogate, string seperator = "/")
        {
            var transform = surrogate.Transform;

            var builder = new StringBuilder();

            builder.Append(transform.name);

            transform = transform.parent;

            while (transform != null)
            {
                builder.Insert(0, seperator);
                builder.Insert(0, transform.name);

                transform = transform.parent;
            }

            return builder.ToString();
        }

        #region Types
        public static IEnumerable<Type> IterateHierarchy(Type type)
        {
            while (true)
            {
                yield return type;

                type = type.BaseType;

                if (type == null) break;
            }
        }

        public static IEnumerable<T> IterateNest<T>(T target, Func<T, T> extract)
            where T : class
        {
            while (true)
            {
                yield return target;

                target = extract(target);

                if (target == null) break;
            }
        }

        public static Type GetCollectionArgument(Type type)
        {
            if (type.IsArray)
                return type.GetElementType();

            return type.GetGenericArguments()[0];
        }
        #endregion

        public static Thread ASyncThread(Func<Task> function)
        {
            void Run() => function().Wait();

            return new Thread(Run);
        }
    }

    public static partial class MUtilityExtensions
    {
        public static string ToPrettyString<T>(this T value) => MUtility.PrettifyName(value);

        public static Color SetAlpha(this Color color, float value)
        {
            color.a = value;

            return color;
        }

        public static string GetHierarchyPath(this Transform transform) => MUtility.GetHierarchyPath(transform);

        public static TDelegate CreateDelegate<TDelegate>(this MethodInfo method)
            where TDelegate : Delegate
        {
            return method.CreateDelegate(typeof(TDelegate)) as TDelegate;
        }
        public static TDelegate CreateDelegate<TDelegate>(this MethodInfo method, object target)
            where TDelegate : Delegate
        {
            return method.CreateDelegate(typeof(TDelegate), target) as TDelegate;
        }

        public static void SetOrAdd<T>(this List<T> list, int index, T item)
        {
            while (list.ValidateCollectionBounds(index) == false)
                list.Add(default);

            list[index] = item;
        }

        public static T GetRandomElement<T>(IList<T> list) => MUtility.GetRandomElement(list);

        #region String
        public static string Join(this IEnumerable<string> collection, string seperator) => string.Join(seperator, collection);
        public static string Join(this IEnumerable<string> collection, char seperator) => Join(collection, seperator.ToString());
        public static string Join(this IEnumerable<string> collection) => Join(collection, "");

        public static string RemoveAll(this string text, params string[] phrases)
        {
            var builder = new StringBuilder(text);

            for (int i = 0; i < phrases.Length; i++)
                builder.Replace(phrases[i], string.Empty);

            return builder.ToString();
        }
        public static string RemoveAll(this string text, params char[] characters)
        {
            var set = new HashSet<char>(characters);

            var builder = new StringBuilder(text.Length);

            for (int i = 0; i < text.Length; i++)
            {
                if (set.Contains(text[i]))
                    continue;

                builder.Append(text[i]);
            }

            return builder.ToString();
        }

        public static string Between(this string text, int start, int end) => text.Substring(start, end - start);

        public static string RemoveSuffix(this string text, string prefix)
        {
            if (text.EndsWith(prefix))
                return text.Substring(0, text.Length - prefix.Length);

            return text;
        }

        public static bool BeginsWith(this string text, char character) => text[0] == character;
        public static bool EndsWith(this string text, char character) => text[text.Length - 1] == character;
        #endregion

        public static bool IsAssignableFrom(this Type type, object target) => type.IsAssignableFrom(target?.GetType());
    }
}