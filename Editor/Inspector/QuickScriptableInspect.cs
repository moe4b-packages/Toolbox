using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MB
{
    /// <summary>
    /// Attribute to Apply to Scriptable Objects to Quickly Inspect them in a Separate Inspector Window
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class QuickScriptableInspectAttribute : PropertyAttribute
    {
        #if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(QuickScriptableInspectAttribute))]
        public class Drawer : PropertyDrawer
        {
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
            {
                if (property.objectReferenceValue == null)
                {
                    EditorGUI.PropertyField(rect, property, label);
                }
                else
                {
                    var target = property.objectReferenceValue;
                    
                    var area = MUtility.GUICoordinates.SplitHorizontally(rect, 0, 80f, 20f);
                    
                    EditorGUI.PropertyField(area[0], property, label);
                    
                    if (GUI.Button(area[1], "Inspect"))
                        QuickScriptableInspect.Show(target);
                }
            }
        }
        #endif
    }
    
    #if UNITY_EDITOR
    public class QuickScriptableInspect : EditorWindow
    {
        private Editor inspector;

        private void Set(Object target)
        {
            inspector = Editor.CreateEditor(target);
            titleContent = new GUIContent($"Quick Inspect: {target.name}");
        }

        private Vector2 scroll;
        
        private void OnGUI()
        {
            //Draw Button & Spacer
            {
                var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * 2);
                if (GUI.Button(rect, "Close"))
                    Close();
            }
            
            var area = new Rect(Vector2.zero, position.size);
            area.yMin += EditorGUIUtility.singleLineHeight * 2.5f;
            area.xMin += 5;
            
            GUILayout.BeginArea(area);
            {
                scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
                {
                    inspector.OnInspectorGUI();
                }
                EditorGUILayout.EndScrollView();
            }
            GUILayout.EndArea();
        }
        
        //Static Utility
        
        public static void Show(Object target)
        {
            var window = CreateWindow<QuickScriptableInspect>();
            window.Set(target);
        }
    }
    #endif
}