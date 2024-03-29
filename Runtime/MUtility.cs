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
using System.Diagnostics;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MB
{
    /// <summary>
    /// A collection of random utility functions
    /// </summary>
    public static partial class MUtility
    {
        public abstract class UI : UIUtility { }
        public abstract class IO : IOUtility { }
        public abstract class Bounds : BoundsUtility { }
        public abstract class Layer : LayerUtility { }
        public abstract class PlayerLoop : PlayerLoopUtility { }
        public abstract class Audio : AudioUtility { }
        public abstract class Process : ProcessUtility { }
        public abstract class Type : TypeUtility { }
        public abstract class Text : TextUtility { }
        public abstract class Platform : PlatformUtility { }
        public abstract class Collections : CollectionsUtility { }
        public abstract class Thread : ThreadUtility { }
        public abstract class Exception : ExceptionUtility { }
        public abstract class UObject : UObjectUtility { }
    }

    #region Sub-Classes
    public abstract class UIUtility
    {
        public static class LocalPointerEventData
        {
            static EventSystem System;

            static PointerEventData cache;

            public static PointerEventData Retrieve()
            {
                if (System != EventSystem.current)
                {
                    System = EventSystem.current;
                    cache = new PointerEventData(EventSystem.current);
                }

                Clear(cache);

                return cache;
            }

            static void Clear(PointerEventData data)
            {
                data.eligibleForClick = false;

                data.pointerId = -1;
                data.position = Vector2.zero; // Current position of the mouse or touch event
                data.delta = Vector2.zero; // Delta since last update
                data.pressPosition = Vector2.zero; // Delta since the event started being tracked
                data.clickTime = 0.0f; // The last time a click event was sent out (used for double-clicks)
                data.clickCount = 0; // Number of clicks in a row. 2 for a double-click for example.

                data.scrollDelta = Vector2.zero;
                data.useDragThreshold = true;
                data.dragging = false;
                data.button = PointerEventData.InputButton.Left;

                data.pressure = 0f;
                data.tangentialPressure = 0f;
                data.altitudeAngle = 0f;
                data.azimuthAngle = 0f;
                data.twist = 0f;
                data.radius = Vector2.zero;
                data.radiusVariance = Vector2.zero;
            }
        }

        public static class PointerOverlap
        {
            static List<RaycastResult> Results = new List<RaycastResult>(10);

            public static bool Check() => Check(Input.mousePosition);
            public static bool Check(Vector3 position)
            {
                Results.Clear();

                var eventData = LocalPointerEventData.Retrieve();
                eventData.position = position;

                EventSystem.current.RaycastAll(eventData, Results);

                for (int i = 0; i < Results.Count; i++)
                {
                    if (Results[i].module.TryGetComponent<Canvas>(out var canvas) == false)
                        continue;

                    if (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                        return true;
                }

                return false;
            }
        }
    }
    public static class UIExtensions
    {
        
    }

    public abstract class IOUtility
    {
        public static void EnsureDirectory(string path)
        {
            var extension = Path.GetExtension(path.AsSpan());

            var directory = extension == "" ? new DirectoryInfo(path) : new FileInfo(path).Directory;

            if (directory.Exists == false) directory.Create();
        }
    }

    public abstract class BoundsUtility
    {
        public static Bounds CalculateRenderer(UObjectSurrogate surrogate) => CalculateRenderer(surrogate, true);
        public static Bounds CalculateRenderer(UObjectSurrogate surrogate, bool includeInactive)
        {
            var targets = surrogate.GameObject.GetComponentsInChildren<Renderer>(includeInactive);

            Bounds Extract(Renderer renderer) => renderer.bounds;

            return Calculate(surrogate, targets, Extract);
        }

        public static Bounds CalculateCollider(UObjectSurrogate surrogate) => CalculateCollider(surrogate, true);
        public static Bounds CalculateCollider(UObjectSurrogate surrogate, bool includeInactive)
        {
            var targets = surrogate.GameObject.GetComponentsInChildren<Collider>(includeInactive);

            Bounds Extract(Collider renderer) => renderer.bounds;

            return Calculate(surrogate, targets, Extract);
        }

        public static Bounds CalculateCollider2D(UObjectSurrogate surrogate) => CalculateCollider(surrogate, true);
        public static Bounds CalculateCollider2D(UObjectSurrogate surrogate, bool includeInactive)
        {
            var targets = surrogate.GameObject.GetComponentsInChildren<Collider2D>(includeInactive);

            Bounds Extract(Collider2D renderer) => renderer.bounds;

            return Calculate(surrogate, targets, Extract);
        }

        public static Bounds Calculate<T>(Transform transform, IList<T> list, Func<T, Bounds> extract)
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
    }

    public abstract class LayerUtility
    {
        public static void Set(UObjectSurrogate surrogate, string name)
        {
            var index = LayerMask.NameToLayer(name);

            Set(surrogate, index);
        }

        public static void Set(UObjectSurrogate surrogate, int index)
        {
            surrogate.GameObject.layer = index;

            for (int i = 0; i < surrogate.Transform.childCount; i++)
                Set(surrogate.Transform.GetChild(i), index);
        }
    }

    public abstract class PlayerLoopUtility
    {
        public static void Register<TType>(PlayerLoopSystem.UpdateFunction callback)
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();

            var index = Locate<TType>(ref loop);

            if (index == -1)
                throw new Exception($"No PlayerLoop Entry Found for {typeof(TType)}");

            loop.subSystemList[index].updateDelegate += callback;

            PlayerLoop.SetPlayerLoop(loop);

            Application.quitting += () => Unregister<TType>(callback);
        }

        public static int Locate<TType>(ref PlayerLoopSystem loop)
        {
            for (int i = 0; i < loop.subSystemList.Length; ++i)
                if (loop.subSystemList[i].type == typeof(TType))
                    return i;

            return -1;
        }

        public static bool Unregister<TType>(PlayerLoopSystem.UpdateFunction callback)
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();

            var index = Locate<TType>(ref loop);

            if (index == -1) return false;

            loop.subSystemList[index].updateDelegate -= callback;

            PlayerLoop.SetPlayerLoop(loop);
            return true;
        }
    }

    public abstract class AudioUtility
    {
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
    }

    public abstract class ProcessUtility
    {
        public static string FormatArguments(params string[] arguments)
        {
            const char Marker = '"';

            if (arguments.Length == 0) return "";

            using (DisposablePool.StringBuilder.Lease(out var builder))
            {
                for (int i = 0; i < arguments.Length; i++)
                {
                    var argument = $"{Marker}{arguments[i]}{Marker}";
                    builder.Append(argument);

                    if (i < arguments.Length - 1) builder.Append(' ');
                }

                return builder.ToString();
            }
        }

        public static ProcessStartInfo FormatSystemCommand(string command)
        {
            var info = new ProcessStartInfo();

            switch (Application.platform)
            {
                //Windows
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsServer:
                {
                    info.FileName = "cmd.exe";

                    command = FormatArguments(command);
                    info.Arguments = $"/C {command}";

                    return info;
                }

                //Linux
                case RuntimePlatform.LinuxEditor:
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxServer:
                {
                    info.FileName = "/bin/bash";

                    command = command.Replace(@"\", @"/");
                    command = FormatArguments(command);
                    info.Arguments = $"-c {command}";

                    return info;
                }

                //OSX
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXServer:
                {
                    break;
                }
            }

            throw new Exception($"Unsupported Platform: {Application.platform}");
        }
    }

    public abstract class TypeUtility
    {
        public static string FormatPropertyBackingFieldName(string name) => $"<{name}>k__BackingField";

        public static IEnumerable<Type> IterateHierarchy(Type type)
        {
            while (true)
            {
                yield return type;

                type = type.BaseType;

                if (type == null) break;
            }
        }

        public static IEnumerable<Type> IterateNesting(Type type)
        {
            while (true)
            {
                yield return type;

                type = type.DeclaringType;

                if (type == null) break;
            }
        }

        public static Type GetCollectionArgument(Type type)
        {
            if (type.IsArray)
                return type.GetElementType();

            if (type.IsGenericType)
                return type.GetGenericArguments()[0];

            throw new ArgumentException();
        }
    }
    public static class TypeExtensions
    {
        public static bool IsAssignableFrom(this Type type, object target)
        {
            if (target == null) return false;

            return type.IsAssignableFrom(target.GetType());
        }
        public static bool IsAssignableFrom<T>(this Type type) => type.IsAssignableFrom(typeof(T));
    }

    public abstract class TextUtility
    {
        public static string Prettify<T>(T value)
        {
            var text = value.ToString();

            var capacity = CalculatePrettifyCapacity(text);
            var builder = new StringBuilder(capacity);

            PrettifyInternal(text, builder);

            return builder.ToString();
        }
        public static void Prettify<T>(T value, StringBuilder builder)
        {
            var text = value.ToString();

            var capacity = CalculatePrettifyCapacity(text);

            builder.EnsureExtraCapacity(capacity);

            PrettifyInternal(text, builder);
        }

        static void PrettifyInternal(string text, StringBuilder builder)
        {
            builder.Append(text[0]);

            for (int i = 1; i < text.Length - 1; i++)
            {
                var previous = text[i - 1];
                var current = text[i];
                var next = text[i + 1];

                if (char.IsUpper(current))
                {
                    if (char.IsLower(previous))
                        builder.Append(' ');
                    else if (char.IsUpper(previous) && char.IsLower(next))
                        builder.Append(' ');
                }

                if (char.IsNumber(current))
                {
                    if (!char.IsNumber(previous))
                        builder.Append(' ');
                }
                else
                {
                    if (char.IsNumber(previous))
                        builder.Append(' ');
                }

                if (current == '_') current = ' ';

                builder.Append(current);
            }

            builder.Append(text[^1]);
        }

        static int CalculatePrettifyCapacity(string text)
        {
            var value = text.Length;

            for (int i = 0; i < text.Length; i++)
                if (char.IsUpper(text[i]) || char.IsDigit(text[i]))
                    value += 1;

            return value;
        }
    }
    public static class TextExtensions
    {
        public static string ToPrettyString<T>(this T value) => MUtility.Text.Prettify(value);

        public static string Join(this IEnumerable<string> collection, string seperator) => string.Join(seperator, collection);
        public static string Join(this IEnumerable<string> collection, char seperator) => Join(collection, seperator.ToString());
        public static string Join(this IEnumerable<string> collection) => Join(collection, "");

        public static string Remove(this string text, params string[] phrases)
        {
            var builder = new StringBuilder(text);

            for (int i = 0; i < phrases.Length; i++)
                builder.Replace(phrases[i], string.Empty);

            return builder.ToString();
        }
        public static string Remove(this string text, params char[] characters)
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

        public static string RemovePrefix(this string text, string prefix)
        {
            if (text.StartsWith(prefix))
            {
                return text.Substring(prefix.Length, text.Length - prefix.Length);
            }

            return text;
        }
        public static string RemoveSuffix(this string text, string suffix)
        {
            if (text.EndsWith(suffix))
                return text.Substring(0, text.Length - suffix.Length);

            return text;
        }

        public static bool BeginsWith(this string text, char character) => text[0] == character;
        public static bool BeginsWith(this string text, string target)
        {
            if (target.Length > text.Length) return false;

            for (int i = 0; i < target.Length; i++)
                if (text[i] != target[i])
                    return false;

            return true;
        }

        public static bool EndsWith(this string text, char character) => text[text.Length - 1] == character;
        public static bool EndsWith(this string text, string target)
        {
            if (target.Length > text.Length) return false;

            var offset = text.Length - target.Length;

            for (int i = 0; i < target.Length; i++)
                if (text[i + offset] != target[i])
                    return false;

            return true;
        }

        public static void EnsureExtraCapacity(this StringBuilder builder, int capacity) => builder.EnsureCapacity(builder.Length + capacity);
    }

    public abstract class PlatformUtility
    {
        public static RuntimePlatform Check()
        {
#if UNITY_EDITOR
            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.StandaloneOSX:
                    return RuntimePlatform.OSXEditor;

                case BuildTarget.StandaloneWindows:
                    return RuntimePlatform.WindowsEditor;

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
                    return RuntimePlatform.LinuxEditor;

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
    }

    public abstract class CollectionsUtility
    {
        public static bool ValidateBounds(int length, int index)
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
    }
    public static class CollectionsExtensions
    {
        /// <summary>
        /// Returns element at index, or returns default if index out of bounds
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static T SafeIndex<T>(this IList<T> collection, int index)
        {
            return SafeIndex(collection, index, default);
        }
        /// <summary>
        /// Returns element at index, or returns fallback if index out of bounds
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="index"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        public static T SafeIndex<T>(this IList<T> collection, int index, T fallback)
        {
            if (ValidateBounds(collection, index) == false) return fallback;

            return collection[index];
        }

        public static bool TryGet<T>(this IList<T> collection, int index, out T value)
        {
            if (collection.ValidateBounds(index) == false)
            {
                value = default;
                return false;
            }

            value = collection[index];
            return true;
        }

        public static bool ValidateBounds<T>(this ICollection<T> collection, int index)
        {
            return MUtility.Collections.ValidateBounds(collection.Count, index);
        }

        public static T GetRandom<T>(this IList<T> list)
        {
            var index = Random.Range(0, list.Count);

            return list[index];
        }

        public static void SetOrAdd<T>(this List<T> list, int index, T item)
        {
            while (list.ValidateBounds(index) == false)
                list.Add(default);

            list[index] = item;
        }

        public static void ForAll<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
                action(item);
        }

        public static T FirstOr<T>(this IEnumerable<T> source, T fallback)
        {
            if (source == null) return fallback;

            if (source is IList<T> list)
            {
                if (list.Count > 0)
                    return list[0];
            }
            else
            {
                foreach (var item in source)
                    return item;
            }

            return fallback;
        }
        public static T LastOr<T>(this IEnumerable<T> source, T fallback)
        {
            if (source == null) return fallback;

            if (source is IList<T> list)
            {
                if (list.Count > 0)
                    return list[list.Count - 1];
                else
                    return fallback;
            }
            else
            {
                var result = fallback;

                foreach (var item in source)
                    result = item;

                return result;
            }
        }

        public static int IndexOf<T>(this IEnumerable<T> source, T target)
        {
            int index = 0;
            foreach (var item in source)
            {
                if (Equals(item, target))
                    return index;

                index += 1;
            }

            return -1;
        }

        public static void AddAll<Tkey, TValue>(this IDictionary<Tkey, TValue> dictionary, IList<TValue> list, Func<TValue, Tkey> keyer)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var key = keyer(list[i]);

                dictionary.Add(key, list[i]);
            }
        }

        public static void EnsureExtraCapacity<T>(this List<T> list, int capacity)
        {
            capacity += list.Count;

            if (list.Capacity < capacity)
                list.Capacity = capacity;
        }

        public static IEnumerable<(T item, int index)> IterateWithIndex<T>(this IEnumerable<T> source)
        {
            if (source is IList<T> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    yield return (list[i], i);
                }
            }
            else
            {
                int index = 0;

                foreach (var item in source)
                {
                    yield return (item, index);
                    index += 1;
                }
            }
        }

        public static IEnumerable<T> Infinite<T>(this IEnumerable<T> source)
        {
            while (true)
            {
                foreach (var item in source)
                {
                    yield return item;
                }
            }
        }
    }

    public abstract class ThreadUtility
    {
        public static Thread ASyncThread(Func<Task> function)
        {
            void Run() => function().Wait();

            return new Thread(Run);
        }
    }

    public abstract class ExceptionUtility
    {
        public static Exception FormatDependencyException<TDependency>(object dependent)
        {
            var text = $"Invalid Dependency of {typeof(TDependency)} by {dependent}";

            return new Exception(text);
        }
    }

    public abstract class UObjectUtility
    {
        public static string GetHierarchyPath(UObjectSurrogate surrogate, string seperator = "/")
        {
            var builder = new StringBuilder();
            GetHierarchyPath(surrogate, builder, seperator);
            return builder.ToString();
        }
        public static void GetHierarchyPath(UObjectSurrogate surrogate, StringBuilder builder, string seperator = "/")
        {
            var transform = surrogate.Transform;

            builder.Append(transform.name);

            transform = transform.parent;

            while (transform != null)
            {
                builder.Insert(0, seperator);
                builder.Insert(0, transform.name);

                transform = transform.parent;
            }
        }

        public static IEnumerable<Transform> IterateHierarchy(UObjectSurrogate surrogate)
        {
            var transform = surrogate.Transform;

            for (int i = 0; i < transform.childCount; i++)
            {
                var context = transform.GetChild(i);

                yield return context;

                if (context.childCount > 0)
                {
                    foreach (var child in IterateHierarchy(context))
                    {
                        yield return child;
                    }
                }
            }
        }

        public static void SetDirty(Object target)
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(target);
#endif
        }
    }
    public static class UObjectExtensions
    {
        /// <summary>
        /// Retrieves the GameObject of the Rigidbody attached to this Collider,
        /// or the GameObject attached to the collider if no Rigidbody is attached to Collider
        /// </summary>
        /// <param name="collider"></param>
        /// <returns></returns>
        public static GameObject GetRoot(this Collider collider)
        {
            if (collider.attachedRigidbody == null)
                return collider.gameObject;

            return collider.attachedRigidbody.gameObject;
        }

        #region Unity Event
        /// <summary>
        /// Listens to a single invoke of the Unity Action
        /// </summary>
        /// <param name="source"></param>
        /// <param name="callback"></param>
        public static void Once(this UnityEvent source, UnityAction callback)
        {
            source.AddListener(Surrogate);
            void Surrogate()
            {
                source.RemoveListener(Surrogate);
                callback();
            }
        }

        /// <summary>
        /// Listens to a single invoke of the Unity Action
        /// </summary>
        /// <param name="source"></param>
        /// <param name="callback"></param>
        public static void Once<T1>(this UnityEvent<T1> source, UnityAction<T1> callback)
        {
            source.AddListener(Surrogate);
            void Surrogate(T1 arg1)
            {
                source.RemoveListener(Surrogate);
                callback(arg1);
            }
        }

        /// <summary>
        /// Listens to a single invoke of the Unity Action
        /// </summary>
        /// <param name="source"></param>
        /// <param name="callback"></param>
        public static void Once<T1, T2>(this UnityEvent<T1, T2> source, UnityAction<T1, T2> callback)
        {
            source.AddListener(Surrogate);
            void Surrogate(T1 arg1, T2 arg2)
            {
                source.RemoveListener(Surrogate);
                callback(arg1, arg2);
            }
        }

        /// <summary>
        /// Listens to a single invoke of the Unity Action
        /// </summary>
        /// <param name="source"></param>
        /// <param name="callback"></param>
        public static void Once<T1, T2, T3>(this UnityEvent<T1, T2, T3> source, UnityAction<T1, T2, T3> callback)
        {
            source.AddListener(Surrogate);
            void Surrogate(T1 arg1, T2 arg2, T3 arg3)
            {
                source.RemoveListener(Surrogate);
                callback(arg1, arg2, arg3);
            }
        }

        /// <summary>
        /// Listens to a single invoke of the Unity Action
        /// </summary>
        /// <param name="source"></param>
        /// <param name="callback"></param>
        public static void Once<T1, T2, T3, T4>(this UnityEvent<T1, T2, T3, T4> source, UnityAction<T1, T2, T3, T4> callback)
        {
            source.AddListener(Surrogate);
            void Surrogate(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            {
                source.RemoveListener(Surrogate);
                callback(arg1, arg2, arg3, arg4);
            }
        }
        #endregion
    }

    public static class ColorExtensions
    {
        public static Color SetAlpha(this Color color, float value)
        {
            color.a = value;

            return color;
        }
    }

    public static class DelegateExtensions
    {
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
    }

    public static class TaskExtensions
    {
        public static async void Forget(this Task task)
        {
            await task;
        }
        public static async void Forget<T>(this Task<T> task)
        {
            await task;
        }
    }
    #endregion
}