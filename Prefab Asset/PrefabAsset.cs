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
    /// A serializable field that ensures only prefabs assets (no scene assets) are selected
    /// </summary>
    [Serializable]
    public class PrefabAsset
    {
        [SerializeField]
        GameObject asset = default;
        public GameObject Asset => asset;

        public static implicit operator GameObject(PrefabAsset prefab) => prefab.asset;
        public static implicit operator PrefabAsset(GameObject asset) => new PrefabAsset(asset);

        public PrefabAsset() : this(null) { }
        public PrefabAsset(GameObject asset)
        {
            this.asset = asset;
        }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(PrefabAsset))]
        public class Drawer : PersistantPropertyDrawer
        {
            SerializedProperty asset;

            protected override void Init()
            {
                base.Init();

                asset = property.FindPropertyRelative(nameof(asset));
            }

            protected override float CalculateHeight()
            {
                return EditorGUIUtility.singleLineHeight;
            }

            protected override void Draw(Rect rect)
            {
                DrawField(rect, label, asset);
            }
        }

        public static void DrawField(Rect rect, GUIContent label, SerializedProperty property)
        {
            property.objectReferenceValue = EditorGUI.ObjectField(rect, label, property.objectReferenceValue, typeof(GameObject), false);
        }
#endif
    }
}