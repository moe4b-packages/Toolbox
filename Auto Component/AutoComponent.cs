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

using System.Reflection;

using static MB.AutoComponentX;

namespace MB
{
	/// <summary>
	/// Serializable Field that will auto retrieve a single/list of component dependency
	/// </summary>
	public static class AutoComponentX
	{
#if UNITY_EDITOR
		public class BaseDrawer : PropertyDrawer
		{
			public string FormatLabelText(string label)
			{
				var prefix = "auto";

				if (label.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
				{
					label = label.Substring(prefix.Length);
					label = label.TrimStart(' ', '_');
					label = char.ToUpper(label[0]) + label.Substring(1);
				}

				return label;
			}
		}
#endif
	}

	#region Single
	[Serializable]
	public abstract class AutoComponent
	{
		[SerializeField]
		protected DependencyScope scope;
		public DependencyScope Scope => scope;

		public enum DependencyScope
		{
			Self = ComponentQueryScope.Self,
			Children = ComponentQueryScope.Children,
			Parents = ComponentQueryScope.Parents,
			Scene = ComponentQueryScope.Scene,
			Global = ComponentQueryScope.Global,
			Manual = 1 << 7,
		}

		public static DependencyScope Self => DependencyScope.Self;
		public static DependencyScope Children => DependencyScope.Children;
		public static DependencyScope Parents => DependencyScope.Parents;
		public static DependencyScope Scene => DependencyScope.Scene;
		public static DependencyScope Global => DependencyScope.Global;
		public static DependencyScope Manual => DependencyScope.Manual;

		public AutoComponent(DependencyScope scope)
		{
			this.scope = scope;
		}

		//Static Utility

		public static DependencyScope ConvertScope(ComponentQueryScope scope) => (DependencyScope)(int)scope;
		public static ComponentQueryScope ConvertScope(DependencyScope scope) => (ComponentQueryScope)(int)scope;

#if UNITY_EDITOR
		[CustomPropertyDrawer(typeof(AutoComponent), true)]
		public class Drawer : BaseDrawer
		{
			SerializedProperty property;

			SerializedProperty component;
			SerializedProperty scope;

			Type type;

			bool isInterface;

			public const float ElementSpacing = 5f;

			void Init(SerializedProperty reference)
			{
				if (property?.propertyPath == reference?.propertyPath) return;

				property = reference;

				type = MUtility.SerializedPropertyType.Retrieve(property).GenericTypeArguments[0];

				isInterface = type.IsInterface;

				component = property.FindPropertyRelative("component");
				scope = property.FindPropertyRelative("scope");
			}

			public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
			{
				Init(property);

				return EditorGUIUtility.singleLineHeight;
			}

			public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
			{
				Init(property);

				label.text = FormatLabelText(label.text);

				DivideRect(rect, out var area1, out var area2);

				DrawField(area1, label);

				DrawScope(area2);
			}

			void DrawField(Rect rect, GUIContent label)
			{
				if (scope.intValue == (int)DependencyScope.Manual)
				{
					if (isInterface)
						EditorGUI.HelpBox(rect, " Cannot Manually Select Interface Component", MessageType.Error);
					else
						EditorGUI.PropertyField(rect, component, label);
				}
				else
				{
					var reference = QueryComponent.In(property.serializedObject.targetObject as Component, type, (ComponentQueryScope)scope.intValue);

					if (isInterface == false && Application.isPlaying == false) component.objectReferenceValue = reference;

					GUI.enabled = false;

					if (isInterface)
						EditorGUI.ObjectField(rect, label, reference, typeof(Component), true);
					else
						EditorGUI.PropertyField(rect, component, label);

					GUI.enabled = true;
				}
			}

			void DrawScope(Rect rect)
			{
				if (Application.isPlaying) GUI.enabled = false;

				EditorGUI.PropertyField(rect, scope, GUIContent.none);

				GUI.enabled = true;
			}

			public static void DivideRect(Rect rect, out Rect area1, out Rect area2)
			{
				area1 = new Rect(rect);
				area1.width *= 0.75f;

				area2 = new Rect(rect);
				area2.width *= 0.25f;
				area2.width -= ElementSpacing;
				area2.x += area1.width;
				area2.x += ElementSpacing;
			}
		}
#endif
	}

	[Serializable]
	public class AutoComponent<T> : AutoComponent
		where T : class
	{
		[SerializeField]
		T component = default;

		bool cached = false;

		public T Retrieve(Component self)
		{
			if (cached == false && scope != DependencyScope.Manual)
			{
				component = QueryComponent.In<T>(self, ConvertScope(scope));

				cached = true;
			}

			return component;
		}

		public static implicit operator AutoComponent<T>(DependencyScope scope) => new AutoComponent<T>(scope);

		public AutoComponent(DependencyScope scope) : base(scope)
		{

		}
	}
	#endregion

	#region Collection
	[Serializable]
	public abstract class AutoComponents
	{
		[SerializeField]
		protected DependencyScope scope;
		public DependencyScope Scope => scope;

		[Flags]
		public enum DependencyScope
		{
			None = 0,

			Everything = ~0,

			Self = ComponentQueryScope.Self,
			Children = ComponentQueryScope.Children,
			Parents = ComponentQueryScope.Parents,
			Scene = ComponentQueryScope.Scene,
			Global = ComponentQueryScope.Global,
		}

		public static DependencyScope Self => DependencyScope.Self;
		public static DependencyScope Children => DependencyScope.Children;
		public static DependencyScope Parents => DependencyScope.Parents;
		public static DependencyScope Scene => DependencyScope.Scene;
		public static DependencyScope Global => DependencyScope.Global;

		public AutoComponents(DependencyScope scope)
		{
			this.scope = scope;
		}

		//Static Utility

		public static DependencyScope ConvertScope(ComponentQueryScope scope) => (DependencyScope)(int)scope;
		public static ComponentQueryScope ConvertScope(DependencyScope scope) => (ComponentQueryScope)(int)scope;

#if UNITY_EDITOR
		[CustomPropertyDrawer(typeof(AutoComponents), true)]
		public class Drawer : BaseDrawer
		{
			SerializedProperty property;

			SerializedProperty list;
			SerializedProperty scope;

			GUIContent label;
			void SetLabel(GUIContent content)
			{
				var text = " " + FormatLabelText(content.text);

				label = new GUIContent(text, content.image, content.tooltip);
			}

			Type type;
			bool isInterface;

			ReorderableList gui;
			ReorderableList.Defaults defaults;

			public bool Expanded
			{
				get
				{
					return property.isExpanded;
				}
				set
				{
					property.isExpanded = value;
				}
			}

			public const float Padding = 20;

			public const float ElementSpacing = 5f;

			void Init(SerializedProperty reference)
			{
				if (property?.propertyPath == reference?.propertyPath) return;

				property = reference;

				list = property.FindPropertyRelative("list");
				scope = property.FindPropertyRelative("scope");

				type = MUtility.SerializedPropertyType.Retrieve(property).GenericTypeArguments[0];
				isInterface = type.IsInterface;

				if (Application.isPlaying && isInterface == false)
					gui = new ReorderableList(list.serializedObject, list, false, true, true, true);
				else
					gui = new ReorderableList(null, type, false, true, true, true);

				gui.drawHeaderCallback = DrawHeader;
				gui.drawElementCallback = DrawElement;

				defaults = new ReorderableList.Defaults();

				if (Application.isPlaying == false || isInterface) UpdateComponents();
			}

			void UpdateComponents()
			{
				gui.list = QueryComponents.In(property.serializedObject.targetObject as Component, type, (ComponentQueryScope)scope.intValue).ToList();
			}

			public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
			{
				Init(property);

				var height = 0f;

				height += Expanded ? gui.GetHeight() : gui.headerHeight;

				height += Padding;

				return height;
			}

			public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
			{
				rect = EditorGUI.IndentedRect(rect);
				EditorGUI.indentLevel = 0;

				rect.y += Padding / 2;
				rect.height -= Padding;

				Init(property);

				SetLabel(label);

				if (Expanded)
					DrawList(ref rect);
				else
					DrawHeader(rect, true);
			}

			void DrawList(ref Rect rect)
			{
				GUI.enabled = false;

				UpdateComponents();

				gui.DoList(rect);

				GUI.enabled = true;
			}

			void DrawHeader(Rect rect) => DrawHeader(rect, false);
			void DrawHeader(Rect rect, bool complete)
			{
				GUI.enabled = true;

				if (complete)
				{
					defaults.DrawHeaderBackground(rect);

					rect.x += 6;
				}

				rect.x += 10f;
				rect.width -= 200;

				Expanded = EditorGUI.Foldout(rect, Expanded, label, true);

				DrawScope(ref rect);

				GUI.enabled = false;
			}

			void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
			{
				var label = new GUIContent($"Element {index}");

				if (gui.list == null)
				{
					var element = gui.serializedProperty.GetArrayElementAtIndex(index);

					EditorGUI.PropertyField(rect, element, label);
				}
				else
				{
					var instance = gui.list[index] as Object;

					EditorGUI.ObjectField(rect, label, instance, type, true);
				}
			}

			void DrawScope(ref Rect rect)
			{
				if (Application.isPlaying) GUI.enabled = false;

				var labelWidth = EditorGUIUtility.labelWidth;
				EditorGUIUtility.labelWidth = 50;

				rect.x += rect.width - 128 + 200;
				rect.width = 120;

				if (Expanded == false)
				{
					rect.x -= 12;
					rect.y += 1;
				}

				EditorGUI.PropertyField(rect, scope);

				EditorGUIUtility.labelWidth = labelWidth;

				GUI.enabled = false;
			}
		}
#endif
	}

	[Serializable]
	public class AutoComponents<T> : AutoComponents
		where T : class
	{
		[SerializeField]
		List<T> list = default;

		bool cached = false;

		public List<T> Retrieve(Component self)
		{
			if (cached == false)
			{
				list = QueryComponents.In<T>(self, ConvertScope(scope)).ToList();

				cached = true;
			}

			return list;
		}

		public static implicit operator AutoComponents<T>(DependencyScope scope) => new AutoComponents<T>(scope);

		public AutoComponents(DependencyScope scope) : base(scope)
		{

		}
	}
	#endregion
}