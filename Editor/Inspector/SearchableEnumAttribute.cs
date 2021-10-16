using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using MB.ThirdParty;
#endif

namespace MB
{
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
                rect = EditorGUI.PrefixLabel(rect, label);

                var name = property.GetEnumValueName();
                var content = new GUIContent(name);

                if (EditorGUI.DropdownButton(rect, content, FocusType.Keyboard))
                {
                    SearchablePopup.Show(rect, property.enumValueIndex, property.enumDisplayNames, OnSelect);

                    void OnSelect(int index)
                    {
                        property.serializedObject.Update();
                        property.enumValueIndex = index;
                        property.serializedObject.ApplyModifiedProperties();
                    }
                }
            }
        }
#endif
    }
}