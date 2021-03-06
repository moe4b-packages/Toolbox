using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
using MB.ThirdParty;
#endif

namespace MB
{
    /// <summary>
    /// Creates a UI where you can search for enum values by their names
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class SearchableEnumAttribute : PropertyAttribute
    {
#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(SearchableEnumAttribute))]
        class Drawer : PropertyDrawer
        {
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
            {
                EditorGUI.BeginProperty(rect, label, property);

                rect = EditorGUI.PrefixLabel(rect, label);

                var name = property.GetEnumValueName();
                var content = new GUIContent(name);

                if (EditorGUI.DropdownButton(rect, content, FocusType.Keyboard))
                {
                    SearchablePopup.Show(rect, property.enumDisplayNames, property.enumValueIndex, OnSelect);
                    void OnSelect(int index)
                    {
                        property.LateModifyProperty(x => x.enumValueIndex = index);
                    }
                }

                EditorGUI.EndProperty();
            }
        }
#endif
    }
}