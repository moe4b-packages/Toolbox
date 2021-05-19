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
	[Serializable]
	public class ComponentInspectorUtility
	{
		[SerializeField]
#pragma warning disable CS0414
		bool selected = default;
#pragma warning restore CS0414

#if UNITY_EDITOR
		[CustomPropertyDrawer(typeof(ComponentInspectorUtility))]
		public class Drawer : PropertyDrawer
		{
			SerializedProperty property;
			SerializedProperty selected;

			void Init(SerializedProperty reference)
			{
				if (property?.propertyPath == reference?.propertyPath) return;

				property = reference;
				selected = property.FindPropertyRelative(nameof(selected));

				Selection.Register(selected);
			}

			public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
			{
				Init(property);

				return EditorGUIUtility.singleLineHeight;
			}

			public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
			{
				Init(property);

				if (selected.boolValue) DrawHighlight(rect);

				rect.x += 50;
				rect.width -= 100;

				var areas = SplitHorizontally(rect, 3, 5);

				DrawMoveUp(areas[0]);
				DrawDuplicate(areas[1]);
				DrawMoveDown(areas[2]);
			}

			void DrawHighlight(Rect rect)
			{
				var color = new Color(0, 153, 255, 0.5f);

				rect.height /= 2f;
				rect.y += rect.height / 2f;

				EditorGUI.DrawRect(rect, color);
			}

			void DrawMoveUp(Rect rect)
			{
				if (GUI.Button(rect, "Move Up"))
				{
					Selection.Set(selected);

					foreach (Component component in property.serializedObject.targetObjects)
						ComponentUtility.MoveComponentUp(component);
				}
			}
			void DrawMoveDown(Rect rect)
			{
				if (GUI.Button(rect, "Move Down"))
				{
					Selection.Set(selected);

					foreach (Component component in property.serializedObject.targetObjects)
						ComponentUtility.MoveComponentDown(component);
				}
			}

			void DrawDuplicate(Rect rect)
			{
				if (GUI.Button(rect, "Duplicate"))
				{
					foreach (Component component in property.serializedObject.targetObjects)
					{
						ComponentUtility.CopyComponent(component);
						ComponentUtility.PasteComponentAsNew(component.gameObject);
					}
				}
			}

			public static Rect[] SplitHorizontally(Rect rect, int cuts, int padding)
			{
				var areas = new Rect[cuts];

				var width = rect.width;
				var x = rect.x;
				var span = width / cuts;

				for (int i = 0; i < cuts; i++)
				{
					areas[i] = new Rect(x + padding, rect.y, span - padding, rect.height);

					x += span;
				}

				return areas;
			}

			public static class Selection
			{
				public static HashSet<SerializedProperty> Collection { get; private set; }

				public static void Register(SerializedProperty property)
				{
					Collection.Add(property);
				}

				public static void Set(SerializedProperty property)
				{
					var removals = new List<SerializedProperty>();

					foreach (var item in Collection)
					{
						try
						{
							item.boolValue = false;
							item.serializedObject.ApplyModifiedPropertiesWithoutUndo();
						}
						catch (Exception)
						{
							removals.Add(item);
						}
					}

					Collection.RemoveWhere(removals.Contains);

					property.boolValue = true;
				}

				static Selection()
				{
					Collection = new HashSet<SerializedProperty>();
				}
			}
		}
#endif
	}
}