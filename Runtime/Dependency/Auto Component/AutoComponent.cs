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
		public class BaseDrawer : PersistantPropertyDrawer
		{
			protected override void Init()
			{
				base.Init();

				FormatLabel(ref label);
			}

			static void FormatLabel(ref GUIContent label)
			{
				var text = label.text;

				var prefix = "auto";

				if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
				{
					text = text.Substring(prefix.Length);
					text = text.TrimStart(' ', '_');
					text = char.ToUpper(text[0]) + text.Substring(1);
				}

				label = new GUIContent(text, label.image, label.tooltip);
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
			Self = QueryComponentScope.Self,
			Children = QueryComponentScope.Children,
			Parents = QueryComponentScope.Parents,
			Scene = QueryComponentScope.Scene,
			Global = QueryComponentScope.Global,
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

		public static DependencyScope ConvertScope(QueryComponentScope scope) => (DependencyScope)(int)scope;
		public static QueryComponentScope ConvertScope(DependencyScope scope) => (QueryComponentScope)(int)scope;

#if UNITY_EDITOR
		[CustomPropertyDrawer(typeof(AutoComponent), true)]
		public class Drawer : BaseDrawer
		{
			SerializedProperty component;
			SerializedProperty scope;

			Type type;
			bool isInterface;

			public const float ElementSpacing = 5f;

			protected override void Init()
			{
				base.Init();

				type = property.MakeSmart().ManagedType;
				isInterface = type.IsInterface;

				component = property.FindPropertyRelative("component");
				scope = property.FindPropertyRelative("scope");
			}

			public override float CalculateHeight()
			{
				return EditorGUIUtility.singleLineHeight;
			}

			public override void Draw(Rect rect)
			{
				var areas = MUtility.GUICoordinates.SplitHorizontally(rect, 0, 75f, 25f);

				DrawField(areas[0], label);
				DrawScope(areas[1]);
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
					var reference = QueryComponent.In(property.serializedObject.targetObject as Component, type, (QueryComponentScope)scope.intValue);

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
				rect.x += ElementSpacing;
				rect.width -= ElementSpacing;

				if (Application.isPlaying) GUI.enabled = false;

				EditorGUI.PropertyField(rect, scope, GUIContent.none);

				GUI.enabled = true;
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

			Self = QueryComponentScope.Self,
			Children = QueryComponentScope.Children,
			Parents = QueryComponentScope.Parents,
			Scene = QueryComponentScope.Scene,
			Global = QueryComponentScope.Global,
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

		public static DependencyScope ConvertScope(QueryComponentScope scope) => (DependencyScope)(int)scope;
		public static QueryComponentScope ConvertScope(DependencyScope scope) => (QueryComponentScope)(int)scope;

#if UNITY_EDITOR
		[CustomPropertyDrawer(typeof(AutoComponents), true)]
		public class Drawer : BaseDrawer
		{
			SerializedProperty list;
			SerializedProperty scope;

			Type type;
			bool isInterface;

			Component component;

			ImprovedReorderableList UI;

			public const float Padding = 20;

			public const float ElementSpacing = 5f;

			protected override void Init()
			{
				base.Init();

				list = property.FindPropertyRelative("list");
				scope = property.FindPropertyRelative("scope");

				type = property.MakeSmart().ManagedType;
				isInterface = type.IsInterface;

				component = SerializedObject.targetObject as Component;

				if (Application.isPlaying && isInterface == false)
					UI = new ImprovedReorderableList(list);
				else
					UI = new ImprovedReorderableList(null, type);

				UI.TitleContent = label;
				UI.ReflectExpandFromProperty(property);

				UI.DrawHeader = DrawHeader;
				UI.DrawTitle = DrawTitle;

				UI.GetElementHeight = GetElementHeight;
				UI.DrawElement = DrawElement;

				if (Application.isPlaying == false || isInterface) UpdateComponents();
			}

			float GetElementHeight(ImprovedReorderableList list, int index)
			{
				return EditorGUIUtility.singleLineHeight;
			}

			void UpdateComponents()
			{
				UI.ManagedList = QueryComponents.In(component, type, (QueryComponentScope)scope.intValue);
			}

			public override float CalculateHeight()
			{
				var height = 0f;

				height += UI.CalculateHeight();
				height += Padding;

				return height;
			}

			public override void Draw(Rect rect)
			{
				rect = EditorGUI.IndentedRect(rect);
				EditorGUI.indentLevel = 0;

				rect.y += Padding / 2;
				rect.height -= Padding;

				DrawList(ref rect);
			}

			void DrawList(ref Rect rect)
			{
				GUI.enabled = false;

				UpdateComponents();
				UI.Draw(rect);

				GUI.enabled = true;
			}

			void DrawHeader(ImprovedReorderableList list, Rect rect)
			{
				UI.DefaultDrawHeader(list, rect);

				DrawScope(rect);
			}

			void DrawTitle(ImprovedReorderableList list, Rect rect)
			{
				GUI.enabled = true;

				UI.DefaultDrawTitle(list, rect);

				GUI.enabled = false;
			}

			void DrawScope(Rect rect)
			{
				GUI.enabled = !Application.isPlaying;

				var labelWidth = EditorGUIUtility.labelWidth;
				EditorGUIUtility.labelWidth = 50;

				rect.y += 1;

				rect.x = rect.width - 200;
				rect.width = 200;

				EditorGUI.PropertyField(rect, scope);

				EditorGUIUtility.labelWidth = labelWidth;

				GUI.enabled = false;
			}

			void DrawElement(ImprovedReorderableList list, Rect rect, int index)
			{
				var label = new GUIContent($"Element {index}");

				switch (UI.Backing)
				{
					case ImprovedReorderableList.BackingType.Serialized:
						var element = UI.Property.GetArrayElementAtIndex(index);
						EditorGUI.PropertyField(rect, element, label);
						break;

					case ImprovedReorderableList.BackingType.Managed:
						var instance = UI.ManagedList[index] as Object;
						EditorGUI.ObjectField(rect, label, instance, type, true);
						break;
				}
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
				list = QueryComponents.In<T>(self, ConvertScope(scope));

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