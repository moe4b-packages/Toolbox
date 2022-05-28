using UnityEngine;

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR
using UnityEditor;
#endif

using System;

namespace MB
{
    /// <summary>
    /// Attribute to be placed on fields of Texture types to show a preview of said textures
    /// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	public class TexturePreviewAttribute : PropertyAttribute
	{
        public float Scale { get; }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(TexturePreviewAttribute))]
		public class Drawer : PropertyDrawer
        {
            public const float FieldsSpacing = 5f;
            public const float PreviewPadding = 2f;
            public const float RightIndent = 10f;

            public void Assign(SerializedProperty property, out TexturePreviewAttribute attribute, out Texture texture)
            {
                attribute = base.attribute as TexturePreviewAttribute;

                if (property.objectReferenceValue == null)
                {
                    texture = default;
                    return;
                }

                texture = property.objectReferenceValue as Texture;

                if (texture.width == 0 || texture.height == 0)
                    texture = default;

                return;
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                var height = EditorGUIUtility.singleLineHeight;

                if(property.isExpanded)
                {
                    Assign(property, out var attribute, out var texture);

                    if (texture != null)
                    {
                        height += CalculatePreviewSize(texture, attribute.Scale).y;
                        height += FieldsSpacing;
                    }
                }

                return height;
            }

            public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
            {
                Assign(property, out var attribute, out var texture);

                DrawField(ref rect, property, label);

                if (texture == null || property.isExpanded == false)
                    return;

                //Spacing
                MUtility.GUICoordinates.SliceLine(ref rect, FieldsSpacing);

                DrawPreview(ref rect, property, texture, attribute.Scale);
            }
            void DrawField(ref Rect rect, SerializedProperty property, GUIContent label)
            {
                var area = MUtility.GUICoordinates.SliceLine(ref rect);

                EditorGUI.BeginProperty(area, label, property);

                //Foldout
                {
                    var space = MUtility.GUICoordinates.SliceHorizontal(ref area, EditorGUIUtility.labelWidth);

                    property.isExpanded = EditorGUI.Foldout(space, property.isExpanded, label, true);
                }

                //Field
                {
                    EditorGUI.PropertyField(area, property, GUIContent.none);
                }

                EditorGUI.EndProperty();
            }
            void DrawPreview(ref Rect rect, SerializedProperty property, Texture texture, float scale)
            {

                rect = EditorGUI.IndentedRect(rect);

                rect.xMin += RightIndent;

                var size = CalculatePreviewSize(texture, scale);

                var area = new Rect(rect.x, rect.y, size.x, size.y);

                EditorGUI.DrawRect(area, Color.grey);

                area.xMin += PreviewPadding;
                area.xMax -= PreviewPadding;
                area.yMin += PreviewPadding;
                area.yMax -= PreviewPadding;

                EditorGUI.DrawTextureTransparent(area, texture);
            }

            public Vector2 CalculatePreviewSize(Texture texture, float scale)
            {
                var aspect = texture.width / 1f / texture.height;

                var total = Screen.width - RightIndent - (EditorGUI.indentLevel * 25f) - 50;

                var width = total * scale;
                var height = width * aspect;

                return new Vector2(width, height);
            }
        }
#endif

        public TexturePreviewAttribute() : this(0.4f) { }
        public TexturePreviewAttribute(float scale)
        {
            this.Scale = scale;
        }
	}
}