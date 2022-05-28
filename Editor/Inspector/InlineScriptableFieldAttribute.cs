using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MB
{
	/// <summary>
	/// Inlines a field containing a Scriptable Object to be editable directly
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	public class InlineScriptableFieldAttribute : PropertyAttribute
	{
		#if UNITY_EDITOR
		[CustomPropertyDrawer(typeof(InlineScriptableFieldAttribute))]
		public class Drawer : PropertyDrawer
		{
			public const float TopOffset = 1f;
			
			public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
			{
				var height = 0f;

				height += EditorGUIUtility.singleLineHeight;
				
				if (property.isExpanded && property.objectReferenceValue is ScriptableObject asset)
				{
					height += TopOffset;
					
					var editor = Editor.Retrieve(asset);
					height += editor.CalculateHeight();
				}

				return height;
			}

			public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
			{
				var line = MUtility.GUICoordinates.SliceLine(ref rect);
				
				if (property.objectReferenceValue is ScriptableObject asset)
				{
					EditorGUI.PropertyField(line, property);
					property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, property.displayName, true);
					
					if (property.isExpanded)
					{
						MUtility.GUICoordinates.SliceLine(ref rect, TopOffset);
					
						EditorGUI.indentLevel++;
						rect = EditorGUI.IndentedRect(rect);
						EditorGUI.indentLevel = 0;
					
						var editor = Editor.Retrieve(asset);
						editor.Draw(rect);
					}
				}
				else
				{
					EditorGUI.PropertyField(line, property, label);
				}
			}
		}

		public class Editor
		{
			public ScriptableObject Asset { get; }
			
			public SerializedObject SerializedObject { get; }
			
			public float CalculateHeight()
			{
				var height = 0f;

				foreach (var child in SerializedObject.IterateChildren())
					height += EditorGUI.GetPropertyHeight(child, true);
				
				return height;
			}
			
			public void Draw(Rect rect)
			{
				foreach (var child in SerializedObject.IterateChildren())
				{
					var height = EditorGUI.GetPropertyHeight(child, true);
					var area = MUtility.GUICoordinates.SliceLine(ref rect, height);

					EditorGUI.PropertyField(area, child, true);
				}
				
				SerializedObject.ApplyModifiedProperties();
			}

			public void Update()
			{
				SerializedObject.Update();
			}

			public Editor(ScriptableObject asset)
			{
				this.Asset = asset;
				this.SerializedObject = new SerializedObject(asset);
			}

			//Static Utility
			private static readonly Dictionary<ScriptableObject, Editor> dictionary;

			public static Editor Retrieve(ScriptableObject asset)
			{
				if (dictionary.TryGetValue(asset, out var editor))
					return editor;

				editor = new Editor(asset);

				dictionary[asset] = editor;
				
				return editor;
			}
			
			public static void UpdateAll()
			{
				foreach (var item in dictionary.Values)
					item.Update();
			}

			private static void SelectionChangeCallback() => UpdateAll();
			private static void UndoPerformedCallback() => UpdateAll();
			
			static Editor()
			{
				dictionary = new Dictionary<ScriptableObject, Editor>();
				
				Selection.selectionChanged += SelectionChangeCallback;
				Undo.undoRedoPerformed += UndoPerformedCallback;
			}
		}
		#endif
	}
}