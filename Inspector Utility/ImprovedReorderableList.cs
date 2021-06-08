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

		public Type ElementType { get; protected set; }

		public (GetExpansionDelegate Get, SetExpansionDelegate Set) ExpansionProperty { get; set; }

		public delegate bool GetExpansionDelegate();
		public bool DefaultGetExpansion() => Property.isExpanded;

		public delegate void SetExpansionDelegate(bool value);
		public void DefaultSetExpansion(bool value) => Property.isExpanded = value;

		public bool IsExpanded
		{
			get => ExpansionProperty.Get();
			set => ExpansionProperty.Set(value);
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
		static SerializedProperty GlobalFocus;

		public static Event Event => Event.current;
		public static bool IsRepaintEvent => Event.type == EventType.Repaint;

		public const float OutlinePadding = 3;

		public void UpdateState()
        {
			CalculateHeight();

			GUI.changed = true;
        }

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

				BodyHeight = ElementsHeight + (BodyVerticalPadding * 2f) + (OutlinePadding * 2);
				if (Count == 0) BodyHeight += EditorGUIUtility.singleLineHeight;

				height += BodyHeight;

				height += ToolbarHeight;
			}
			else
            {
				ElementsHeight = 0f;
				ElementsHeights.Clear();

				for (int i = 0; i < Count; i++)
					ElementsHeights.Add(0);
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

			if (Selection >= Count) Selection = -1;
			if (GlobalFocus != Property) IsFocused = false;

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

		public Color PrimaryColor { get; set; } = new Color32(48, 48, 48, 255);

		void HeaderGUI(ref Rect rect)
		{
			var area = MUtility.GUICoordinates.SliceLine(ref rect, HeaderHeight);

			ProcessItemDrop(area);

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
			if(IsDroppingItems)
            {
				EditorGUI.DrawRect(rect, new Color32(44, 93, 135, 255));

				rect.xMin += 3;
				rect.xMax -= 3;
				rect.yMin += 3;
				rect.yMax -= 3;

				return;
			}

			EditorGUI.DrawRect(rect, PrimaryColor);
		}

		public string TitleText { get; set; }
		public GUIStyle TitleStyle { get; set; }
		public Vector2 TitleFoldoutOffset { get; set; } = new Vector2(20f, 0f);

		public delegate void DrawTitleDelegate(Rect rect);
		public DrawTitleDelegate DrawTitle { get; set; }
		public void DefaultDrawTitle(Rect rect)
		{
			IsExpanded = EditorGUI.Foldout(rect, IsExpanded, " " + TitleText , true, TitleStyle);
		}
		#endregion

		#region Item Drop
		public bool SupportItemDrop { get; set; } = true;

		public bool IsDroppingItems { get; private set; }

		void ProcessItemDrop(Rect area)
		{
			if (SupportItemDrop == false) return;

			var WithinBounds = area.Contains(Event.mousePosition);

			if (Event.rawType == EventType.DragUpdated)
			{
				IsDroppingItems = ValidateItemDrop() && WithinBounds;

				if (IsDroppingItems)
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
			}
			else if (Event.rawType == EventType.DragPerform)
			{
				if (IsDroppingItems)
				{
					DragAndDrop.AcceptDrag();
					ApplyItemDrop();
				}

				IsDroppingItems = false;
			}
		}

		bool ValidateItemDrop()
		{
			foreach (var item in DragAndDrop.objectReferences)
			{
				if (ElementType.IsAssignableFrom(item))
					continue;

				if (typeof(Component).IsAssignableFrom(ElementType) && item is GameObject)
					continue;

				return false;
			}

			return true;
		}

		void ApplyItemDrop()
        {
			var targets = new List<Object>();

			foreach (var item in DragAndDrop.objectReferences)
			{
				if (item is GameObject && typeof(Component).IsAssignableFrom(ElementType))
				{
					var range = QueryComponents.InSelf((item as GameObject), ElementType);
					targets.AddRange(range);
				}
				else
				{
					targets.Add(item);
				}
			}

			foreach (var target in targets)
			{
				var index = Count;
				Property.InsertArrayElementAtIndex(index);

				var element = Property.GetArrayElementAtIndex(index);
				element.objectReferenceValue = target;

				Selection = index;
			}

			Undo.SetCurrentGroupName($"Drop Items to {TitleText}");

			InvokeElementChange();

			UpdateState();
		}
		#endregion

		#region Body GUI
		public float BodyHeight { get; private set; } = 0f;

		public float BodyVerticalPadding { get; set; } = 5;

		void BodyGUI(ref Rect rect)
		{
			var area = MUtility.GUICoordinates.SliceLine(ref rect, BodyHeight);

			EditorGUI.DrawRect(area, PrimaryColor);

			area.yMax -= OutlinePadding;
			area.xMin += OutlinePadding;
			area.xMax -= OutlinePadding;

			var color = new Color32(64, 65, 65, 255);
			EditorGUI.DrawRect(area, color);

			MUtility.GUICoordinates.SliceLine(ref area, BodyVerticalPadding);

			if (Count == 0)
				DrawEmptyIndicator(area);
			else
				ElementsGUI(area);
		}

		void DrawEmptyIndicator(Rect rect)
        {
			rect.x += 15;
			rect.y -= BodyVerticalPadding - OutlinePadding;

			EditorGUI.LabelField(rect, "List is Empty");
		}
		#endregion

		#region Element GUI
		public float ElementsHeight { get; private set; } = 0f;
		public List<float> ElementsHeights { get; private set; } = new List<float>();

		List<Rect> ElementsRects = new List<Rect>()
		{
			default,
			default,
			default,
			default,
			default,
			default,
			default,
		};

		public float ElementVerticalPadding { get; set; } = 2f;
		public float ElementHorizontalPadding { get; set; } = 10f;

		public const float ElementHandleSize = 10f;
		public const float ElementHandlePadding = 5f;

		void ElementsGUI(Rect rect)
		{
			for (int i = 0; i < Count; i++)
			{
				var area = MUtility.GUICoordinates.SliceLine(ref rect, ElementsHeights[i]);

				ElementGUI(area, i);
			}
		}

		void ElementGUI(Rect area, int index)
        {
			if (Event.rawType == EventType.Repaint) ElementsRects.SetOrAdd(index, area);

			ProcessElementMouseSelection(area, index);

			DrawElementBackground(area, index);

			if (IsDraggingElement && DragElementDestination == index) DrawElementDragIndication(area);

			area.x += ElementHorizontalPadding / 2f;
			area.width -= ElementHorizontalPadding;

			area.y += ElementVerticalPadding;
			area.height -= ElementVerticalPadding * 2f;

			DrawElementHandle(area, index);

			area.x += ElementHandleSize + ElementHandlePadding;
			area.width -= ElementHandleSize + ElementHandlePadding;

			DrawElement(area, index);
		}

		void ProcessElementMouseSelection(Rect rect, int index)
		{
			if (Event.isMouse == false) return;
			if (Event.button != 0) return;

			var InsideBounds = rect.Contains(Event.mousePosition);

			if (InsideBounds)
			{
				if (Event.rawType == EventType.MouseDown)
				{
					Selection = index;
					GlobalFocus = Property;
				}
			}

			if (Selection == index)
			{
				if (InsideBounds && Event.rawType == EventType.MouseDrag && IsDraggingElement == false)
				{
					BeginElementDrag();
				}

				if (IsDraggingElement)
				{
					if (Event.rawType == EventType.MouseDrag)
						UpdateElementDrag();
					else
						EndElementDrag();
				}
			}

			GUI.changed = true;
		}

		public const float DragIndicationHeight = 5f;

		void DrawElementDragIndication(Rect rect)
		{
			if (DragElementDirection > 0)
			{
				rect.y -= DragIndicationHeight / 2f;
				rect.height = DragIndicationHeight;
			}
			else if (DragElementDirection < 0)
			{
				rect.y += rect.height - (DragIndicationHeight / 2f);
				rect.height = DragIndicationHeight;
			}
			else
				return;

			var color = new Color32(44, 93, 135, 255);

			EditorGUI.DrawRect(rect, color);
		}

		void DrawElementBackground(Rect rect, int index)
		{
			if (Selection == index)
			{
				var color = IsFocused ? new Color32(44, 93, 135, 255) : new Color32(80, 80, 80, 255);

				EditorGUI.DrawRect(rect, color);
			}
		}

		public GUIStyle ElementDragHandleStyle = "RL DragHandle";
		public GUIStyle ElementIndicatorArrowStyle = "ArrowNavigationRight";

		public bool CenterElementHandle { get; set; } = true;

		void DrawElementHandle(Rect rect, int index)
		{
			var area = new Rect(rect.x, rect.y, ElementHandleSize, ElementHandleSize);

			if (CenterElementHandle)
				area.y += (rect.height / 2f) - (area.height / 2f);
			else
				area.y += 5f;

			GUIStyle style;

			if (DragElementDestination == index)
			{
				style = ElementIndicatorArrowStyle;

				area.x -= 3f;
				area.y -= 3f;
			}
            else
            {
				style = ElementDragHandleStyle;

				area.y += 3f;
			}

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

        #region Element Dragging
		public bool IsDraggingElement { get; private set; }

		int DragElementSource = -1;
		int DragElementDestination = -1;

		public int DragElementDirection => DragElementSource - DragElementDestination;

		void BeginElementDrag()
		{
			IsDraggingElement = true;

			DragElementSource = Selection;
			DragElementDestination = DragElementSource;

			//Debug.Log($"Begin Drag: {DragSource}");
		}

		void UpdateElementDrag()
		{
			for (int i = 0; i < ElementsRects.Count; i++)
			{
				if (ElementsRects[i].Contains(Event.mousePosition))
				{
					DragElementDestination = i;
					break;
				}
			}

			//Debug.Log($"Update Drag: {DragSource} -> {DragDestination}");
		}

		void EndElementDrag()
		{
			//Debug.Log($"End Drag: {DragSource} -> {DragDestination}");

			if (DragElementSource != DragElementDestination)
			{
				ReorderElement(DragElementSource, DragElementDestination);
				Undo.SetCurrentGroupName($"Reorder {TitleText} Element");

				InvokeElementChange();
			}

			IsDraggingElement = false;

			DragElementSource = -1;
			DragElementDestination = -1;
		}
		#endregion

		#region Toolbar GUI
		public const float ToolbarHeight = 24f;
		public const float ToolbarWidth = 80f;

		void ToolbarGUI(ref Rect rect)
		{
			var area = MUtility.GUICoordinates.SliceLine(ref rect, ToolbarHeight);

			area.x = area.width - ToolbarWidth;
			area.width = ToolbarWidth;

			DrawToolbar(area);

			area.yMin += OutlinePadding;

			var areas = MUtility.GUICoordinates.SplitHorizontally(area, 5, 2);
			DrawToolbarAdd(areas[0]);
			DrawToolbarRemove(areas[1]);
		}

		void DrawToolbar(Rect rect)
		{
			EditorGUI.DrawRect(rect, PrimaryColor);

			rect.yMax -= OutlinePadding;
			rect.xMin += OutlinePadding;
			rect.xMax -= OutlinePadding;

			var color = new Color32(64, 65, 65, 255);
			EditorGUI.DrawRect(rect, color);
		}

		public static GUIStyle ToolbarButtonStyle = "RL FooterButton";

		public static GUIContent ToolbarSimplePlusContent = EditorGUIUtility.TrIconContent("Toolbar Plus");
		public static GUIContent ToolbarMorePlusContent = EditorGUIUtility.TrIconContent("Toolbar Plus More");
		public bool DrawMorePlus { get; set; } = false;
		void DrawToolbarAdd(Rect rect)
		{
			var content = DrawMorePlus ? ToolbarMorePlusContent : ToolbarSimplePlusContent;

			if (GUI.Button(rect, content, ToolbarButtonStyle))
			{
				var index = Selection;
				if (index < 0) index = Count;

				AddElement(index);
				Undo.SetCurrentGroupName($"Add Element to {TitleText}");

				InvokeElementChange();
			}
		}

		public static GUIContent ToolbarMinusContent = EditorGUIUtility.TrIconContent("Toolbar Minus");
		void DrawToolbarRemove(Rect rect)
		{
			if (Selection < 0) GUI.enabled = false;

			if (GUI.Button(rect, ToolbarMinusContent, ToolbarButtonStyle))
            {
				RemoveElement(Selection);
				Undo.SetCurrentGroupName($"Remove Element From {TitleText}");

				InvokeElementChange();
			}

			GUI.enabled = true;
		}
		#endregion

		#region Controls
		public delegate void AddElementDelegate(int index);
		public AddElementDelegate AddElement { get; set; }
		public void DefaultAddElement(int index)
		{
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

		public delegate void ReorderElementDelegate(int source, int destination);
		public ReorderElementDelegate ReorderElement { get; set; }
		public void DefaultReorderElement(int source, int destination)
		{
			Property.MoveArrayElement(source, destination);
		}

		public delegate void ChangeElementDelegate();
		public event ChangeElementDelegate OnChangeElement;
		void InvokeElementChange()
		{
			OnChangeElement?.Invoke();
		}
		#endregion

		public ImprovedReorderableList(SerializedProperty property)
		{
			this.Property = property;

			ElementType = MUtility.SerializedPropertyType.Retrieve(property);
			ElementType = MUtility.GetCollectionArgument(ElementType);

			TitleText = property.displayName;
			TitleStyle = new GUIStyle(EditorStyles.foldout);

			ExpansionProperty = (DefaultGetExpansion, DefaultSetExpansion);

			DrawHeader = DefaultDrawHeader;
			DrawTitle = DefaultDrawTitle;

			GetElementHeight = DefaultGetElementHeight;
			DrawElement = DefaultDrawElement;

			RemoveElement = DefaultRemoveElement;
			AddElement = DefaultAddElement;
			ReorderElement = DefaultReorderElement;
		}
	}
}