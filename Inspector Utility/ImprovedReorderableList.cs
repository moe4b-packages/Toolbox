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
	public class ImprovedReorderableList
	{
		public SerializedProperty Property { get; protected set; }
		public SerializedObject SerializedObject => Property.serializedObject;

		public bool IsExpanded
		{
			get => Property.isExpanded;
			set => Property.isExpanded = value;
		}

		public int Count => Property.arraySize;

		int Internal_Selection = -1;
		public int Selection
		{
			get => Internal_Selection;
			set
			{
				Internal_Selection = value;

				IsFocused = true;
			}
		}

		public bool IsFocused { get; protected set; }

		public static Event Event => Event.current;
		public static bool IsRepaintEvent => Event.type == EventType.Repaint;

        #region Calculate Height
        public float CalculateHeight()
		{
			var height = 0f;

			height += HeaderHeight;

			if (IsExpanded)
			{
				ElementsHeight = 0f;
				ElementsHeights.Clear();

				for (int i = 0; i < Count; i++)
				{
					var value = GetElementHeight(i) + (ElementVerticalPadding * 2f);

					ElementsHeight += value;
					ElementsHeights.Add(value);
				}

				BodyHeight = ElementsHeight + (BodyVerticalPadding * 2f);

				height += BodyHeight;

				height += ToolbarHeight;
			}

			return height;
		}

		public delegate float GetElementHeightCallback(int index);
		public GetElementHeightCallback GetElementHeight { get; set; }
		public float DefaultGetElementHeight(int index)
		{
			var target = Property.GetArrayElementAtIndex(index);

			return EditorGUI.GetPropertyHeight(target, true);
		}
        #endregion

        public virtual void Draw(Rect rect)
		{
			ProcessElementsSelection(rect);

			rect = EditorGUI.IndentedRect(rect);
			EditorGUI.indentLevel = 0;

			if (Selection >= Count) Selection = Count - 1;

			HeaderGUI(ref rect);

			if (IsExpanded)
			{
				BodyGUI(ref rect);
				ToolbarGUI(ref rect);
			}
		}

		void ProcessElementsSelection(Rect rect)
		{
			if (Selection < 0) return;

			if (Event.isMouse && Event.rawType == EventType.MouseDown)
			{
				if (IsFocused)
					IsFocused = rect.Contains(Event.mousePosition);
			}

			if (IsFocused == false) return;

			if (Event.isKey && Event.rawType == EventType.KeyDown)
			{
				if (Event.keyCode == KeyCode.UpArrow)
				{
					if (Selection > 0) Selection--;

					Event.Use();
				}

				if (Event.keyCode == KeyCode.DownArrow)
				{
					if (Selection + 1 < Count) Selection++;

					Event.Use();
				}
			}

			GUI.changed = true;
		}

		#region Heading & Title GUI
		public float HeaderHeight { get; set; } = 20f;
		public GUIStyle HeaderStyle { get; set; } = new GUIStyle(EditorStyles.foldout);

		public string TitleText { get; set; }
		public Vector2 TitleFoldoutOffset { get; set; } = new Vector2(20f, 0f);

		void HeaderGUI(ref Rect rect)
		{
			var area = MUtility.GUICoordinates.SliceLine(ref rect, HeaderHeight);

			DrawHeader(area);

			area.x += TitleFoldoutOffset.x;
			area.width -= TitleFoldoutOffset.x;

			area.y += TitleFoldoutOffset.y;
			area.height -= TitleFoldoutOffset.y;

			DrawTitle(area);
		}

		public delegate void DrawHeaderDelegate(Rect rect);
		public DrawHeaderDelegate DrawHeader { get; set; }
		public void DefaultDrawHeader(Rect rect)
		{
			var color = new Color32(48, 48, 48, 255);

			EditorGUI.DrawRect(rect, color);
		}

		public delegate void DrawTitleDelegate(Rect rect);
		public DrawTitleDelegate DrawTitle { get; set; }
		public void DefaultDrawTitle(Rect rect)
		{
			IsExpanded = EditorGUI.Foldout(rect, IsExpanded, TitleText, true, HeaderStyle);
		}
		#endregion

		#region Body GUI
		public float BodyHeight { get; private set; } = 0f;

		public float BodyVerticalPadding { get; set; } = 5;

		void BodyGUI(ref Rect rect)
		{
			var color = new Color32(64, 65, 65, 255);

			var area = MUtility.GUICoordinates.SliceLine(ref rect, BodyHeight);
			EditorGUI.DrawRect(area, color);

			area.y += BodyVerticalPadding;
			area.height -= BodyVerticalPadding * 2f;

			ElementsGUI(area);
		}
		#endregion

		#region Element GUI
		public float ElementsHeight { get; private set; } = 0f;
		public List<float> ElementsHeights { get; private set; } = new List<float>();

		public float ElementVerticalPadding { get; set; } = 2f;
		public float ElementHorizontalPadding { get; set; } = 10f;

		public const float ElementHandleSize = 10f;
		public const float ElementHandlePadding = 5f;

		void ElementsGUI(Rect rect)
		{
			for (int i = 0; i < Count; i++)
			{
				var area = MUtility.GUICoordinates.SliceLine(ref rect, ElementsHeights[i]);

				DrawElementBackground(area, i);

				area.x += ElementHorizontalPadding / 2f;
				area.width -= ElementHorizontalPadding;

				area.y += ElementVerticalPadding;
				area.height -= ElementVerticalPadding * 2f;

				DrawElementHandle(area);

				area.x += ElementHandleSize + ElementHandlePadding;
				area.width -= ElementHandleSize + ElementHandlePadding;

				DrawElement(area, i);
			}
		}

		public void DrawElementBackground(Rect rect, int index)
		{
			ProcessElementMouseSelection(rect, index);

			if (Selection == index)
			{
				var color = IsFocused ? new Color32(44, 93, 135, 255) : new Color32(80, 80, 80, 255);

				EditorGUI.DrawRect(rect, color);
			}
		}
		void ProcessElementMouseSelection(Rect rect, int index)
		{
			if (Event.isMouse == false) return;
			if (Event.rawType != EventType.MouseDown) return;
			if (Event.button != 0) return;

			if (rect.Contains(Event.mousePosition) == false) return;

			Selection = index;
			IsFocused = true;

			GUI.changed = true;
		}

		void DrawElementHandle(Rect rect)
		{
			GUIStyle style = "RL DragHandle";

			var area = new Rect(rect.x, rect.y, ElementHandleSize, ElementHandleSize);

			area.y += (rect.height / 2f) - (area.height / 2f);
			area.y += 3f;

			if (IsRepaintEvent) style.Draw(area, GUIContent.none, 0);
		}

		public delegate void DrawElementDelegate(Rect rect, int index);
		public DrawElementDelegate DrawElement { get; set; }
		public void DefaultDrawElement(Rect rect, int index)
		{
			var target = Property.GetArrayElementAtIndex(index);

			EditorGUI.PropertyField(rect, target, true);
		}
		#endregion

		#region Toolbar GUI
		public const float ToolbarHeight = 20f;
		public const float ToolbarWidth = 80f;

		void ToolbarGUI(ref Rect rect)
		{
			var area = MUtility.GUICoordinates.SliceLine(ref rect, ToolbarHeight);

			area.x = area.width - ToolbarWidth;
			area.width = ToolbarWidth;

			DrawToolbar(area);

			var areas = MUtility.GUICoordinates.SplitHorizontally(area, 5, 2);
			DrawToolbarAdd(areas[0]);
			DrawToolbarRemove(areas[1]);
		}

		void DrawToolbar(Rect rect)
		{
			var color = new Color32(64, 65, 65, 255);

			EditorGUI.DrawRect(rect, color);
		}

		public static GUIStyle ToolbarButtonStyle = "RL FooterButton";

		public static GUIContent ToolbarPlusContent = EditorGUIUtility.TrIconContent("Toolbar Plus");
		void DrawToolbarAdd(Rect rect)
		{
			if (GUI.Button(rect, ToolbarPlusContent, ToolbarButtonStyle))
			{
				var index = Selection;
				if (index < 0) index = Count;

				AddElement(index);
			}
		}

		public static GUIContent ToolbarMinusContent = EditorGUIUtility.TrIconContent("Toolbar Minus");
		void DrawToolbarRemove(Rect rect)
		{
			if (Selection < 0) GUI.enabled = false;

			if (GUI.Button(rect, ToolbarMinusContent, ToolbarButtonStyle))
				RemoveElement(Selection);

			GUI.enabled = true;
		}
		#endregion

		#region Controls
		public delegate void AddElementDelegate(int index);
		public AddElementDelegate AddElement { get; set; }
		public void DefaultAddElement(int index)
		{
			Debug.Log(index);

			Property.InsertArrayElementAtIndex(index);

			Selection = index + 1;
		}

		public delegate void RemoveElementDelegate(int index);
		public RemoveElementDelegate RemoveElement { get; set; }
		public void DefaultRemoveElement(int index)
		{
			var target = Property.GetArrayElementAtIndex(index);

			if (target.propertyType == SerializedPropertyType.ObjectReference)
				target.objectReferenceValue = null;

			Property.DeleteArrayElementAtIndex(index);

			Selection = index - 1;
		}
		#endregion

		public ImprovedReorderableList(SerializedProperty property)
		{
			this.Property = property;

			TitleText = " " + property.displayName;

			DrawHeader = DefaultDrawHeader;
			DrawTitle = DefaultDrawTitle;

			GetElementHeight = DefaultGetElementHeight;
			DrawElement = DefaultDrawElement;

			RemoveElement = DefaultRemoveElement;
			AddElement = DefaultAddElement;
		}
	}
}