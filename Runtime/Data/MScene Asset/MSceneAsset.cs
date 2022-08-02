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
    /// A serializable field that will contain a reference to a scene asset
    /// </summary>
    [Serializable]
    public class MSceneAsset : ISerializationCallbackReceiver
    {
#if UNITY_EDITOR
        [SerializeField]
        Object asset;
        public Object Asset => asset;
#endif

        [SerializeField]
        bool inBuild;
        public bool InBuild => inBuild;

        [SerializeField]
        bool enabled;
        public bool Enabled => enabled;

        [SerializeField]
        string name;
        public string Name => name;

        [SerializeField]
        int buildIndex;
        public int BuildIndex => buildIndex;

        [SerializeField]
        string path;
        public string Path => path;

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            Refresh();
#endif
        }
        public void OnAfterDeserialize() { }

#if UNITY_EDITOR
        public void Refresh()
        {
            inBuild = TryFind(asset, out enabled, out name, out buildIndex, out path);
        }
#endif

        public override string ToString()
        {
#if UNITY_EDITOR
            return asset == null ? "null" : asset.name;
#else
            return registered ? id : "null";
#endif
        }

#if UNITY_EDITOR
        public MSceneAsset(Object asset)
        {
            this.asset = asset;

            Refresh();
        }
#endif

        public static implicit operator int(MSceneAsset scene) => scene.BuildIndex;

#if UNITY_EDITOR
        //Static Utility

        public static bool TryFind(Object asset, out bool active, out string id, out int index, out string path)
        {
            if (asset != null)
            {
                index = 0;
                path = AssetDatabase.GetAssetPath(asset);

                foreach (var entry in EditorBuildSettings.scenes)
                {
                    if (entry.path == path)
                    {
                        active = entry.enabled;
                        id = asset.name;
                        path = entry.path;
                        return true;
                    }

                    if (entry.enabled) index += 1;
                }
            }

            path = string.Empty;
            id = string.Empty;
            index = 0;
            active = false;
            return false;
        }

        public static bool Register(Object scene)
        {
            var list = EditorBuildSettings.scenes.ToList();

            var path = AssetDatabase.GetAssetPath(scene);

            if (list.Any(x => x.path == path)) return false;

            var entry = new EditorBuildSettingsScene(path, true);

            list.Add(entry);

            EditorBuildSettings.scenes = list.ToArray();

            return true;
        }

        public static void Activate(Object scene)
        {
            var list = EditorBuildSettings.scenes;

            var path = AssetDatabase.GetAssetPath(scene);

            foreach (var entry in list)
            {
                if (entry.path == path)
                {
                    entry.enabled = true;
                    break;
                }
            }

            EditorBuildSettings.scenes = list;
        }

        [CustomPropertyDrawer(typeof(MSceneAsset))]
        public class Drawer : PropertyDrawer
        {
            public static float LineHeight => EditorGUIUtility.singleLineHeight;

            public static void FindProperties(SerializedProperty property, out SerializedProperty asset, out SerializedProperty inBuild, out SerializedProperty enabled)
            {
                asset = property.FindPropertyRelative(nameof(MSceneAsset.asset));
                inBuild = property.FindPropertyRelative(nameof(MSceneAsset.inBuild));
                enabled = property.FindPropertyRelative(nameof(MSceneAsset.enabled));
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                FindProperties(property, out var asset, out var registered, out var active);

                var height = LineHeight;

                if (asset.objectReferenceValue != null)
                {
                    if (registered.boolValue == false)
                        height += LineHeight;
                    else if (active.boolValue == false)
                        height += LineHeight;
                }

                return height;
            }

            public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
            {
                FindProperties(property, out var asset, out var registered, out var active);

                DrawField(ref rect, label, asset);

                if (asset.objectReferenceValue != null)
                {
                    if (registered.boolValue == false)
                        DrawInstruction(ref rect, $"Scene not Registered to Build Settings", MessageType.Warning, "Register", () => Register(asset.objectReferenceValue));
                    else if (active.boolValue == false)
                        DrawInstruction(ref rect, $"Scene not Active within Build Settings", MessageType.Warning, "Activate", () => Activate(asset.objectReferenceValue));
                }
            }

            void Register(Object target) => MSceneAsset.Register(target);
            void Activate(Object target) => MSceneAsset.Activate(target);

            void DrawField(ref Rect rect, GUIContent label, SerializedProperty asset)
            {
                var area = MUtility.GUICoordinates.SliceLine(ref rect);

                asset.objectReferenceValue = EditorGUI.ObjectField(area, label, asset.objectReferenceValue, typeof(SceneAsset), false);
            }

            void DrawInstruction(ref Rect rect, string text, MessageType type, string instructions, Action callback)
            {
                var area = MUtility.GUICoordinates.SliceLine(ref rect);

                area.width -= 90;

                EditorGUI.HelpBox(area, text.Insert(0, " "), type);

                area.x += area.width + 10;
                area.width = 80;

                if (GUI.Button(area, instructions)) callback?.Invoke();
            }
        }

        public class AssetComparer : IEqualityComparer<MSceneAsset>
        {
            public static AssetComparer Instance { get; } = new AssetComparer();

            public bool Equals(MSceneAsset right, MSceneAsset left)
            {
                return right?.asset == left?.asset;
            }

            public int GetHashCode(MSceneAsset target)
            {
                if (target == null) return 0;
                if (target.asset == null) return 0;

                return target.asset.GetHashCode();
            }
        }
#endif
    }
}