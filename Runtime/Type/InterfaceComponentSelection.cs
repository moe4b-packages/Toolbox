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
	/// <summary>
	/// A serializable field that allows you to select a component that implements a specific interface
	/// </summary>
	[Serializable]
	public class InterfaceComponentSelection
	{
#if UNITY_EDITOR
		[CustomPropertyDrawer(typeof(InterfaceComponentSelection), true)]
		public class Drawer : PropertyDrawer
		{
			SerializedProperty FindGameObjectProperty(SerializedProperty property) => property.FindPropertyRelative("gameObject");
			SerializedProperty FindComponentProperty(SerializedProperty property) => property.FindPropertyRelative("component");

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
				return EditorGUIUtility.singleLineHeight;
			}

			void UpdateState(SerializedProperty gameObject, SerializedProperty component, Type type)
			{
				var target = gameObject.objectReferenceValue as GameObject;

				if (target == null)
					component.objectReferenceValue = null;
				else
					component.objectReferenceValue = target.GetComponent(type);
			}

			public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
            {
				var type = property.MakeSmart().ManagedType.GenericTypeArguments[0];

				var gameObject = FindGameObjectProperty(property);
				var component = FindComponentProperty(property);

				var areas = MUtility.GUICoordinates.SplitHorizontally(rect, 5, 70f, 30f);

				EditorGUI.BeginChangeCheck();
				{
					DrawGameObject(areas[0], label, gameObject);
				}
				if (EditorGUI.EndChangeCheck())
					UpdateState(gameObject, component, type);

				DrawComponent(areas[1], gameObject, component, type);
			}

			void DrawGameObject(Rect rect, GUIContent label, SerializedProperty gameObject)
			{
				rect.x -= 2.5f;

				EditorGUI.PropertyField(rect, gameObject, label);
			}

			void DrawComponent(Rect rect, SerializedProperty gameObject, SerializedProperty component, Type type)
			{
				var label = FormatComponentName(component.objectReferenceValue as Component);
				var content = new GUIContent(label);

				if (EditorGUI.DropdownButton(rect, content, FocusType.Passive))
				{
					Dropdown.Show(gameObject.objectReferenceValue as GameObject, type, rect, Handler);

					void Handler(Component target)
                    {
						component.objectReferenceValue = target;
						component.serializedObject.ApplyModifiedProperties();
                    }
				}
			}
		}

		public static class Dropdown
        {
			public delegate void HandlerDelegate(Component component);
			public static void Show(GameObject gameObject, Type type, Rect rect, HandlerDelegate handler)
			{
				var menu = new GenericMenu();

				var components = gameObject.GetComponents(type);

				menu.AddItem(new GUIContent("None"), false, Callback, null);

				for (int i = 0; i < components.Length; i++)
                {
					var label = FormatComponentName(components[i]);
					var content = new GUIContent(label);

					menu.AddItem(content, false, Callback, components[i]);
				}

				void Callback(object target)
                {
					var component = target as Component;

					handler(component);
                }

				menu.DropDown(rect);
			}
		}

		public static string FormatComponentName(Component component)
        {
			if (component == null) return "None";

			return MUtility.PrettifyName(component.GetType().Name);
		}
#endif
	}

	[Serializable]
	public class InterfaceComponentSelection<T> : InterfaceComponentSelection
		where T : class 
	{
		[SerializeField]
		GameObject gameObject;

		[SerializeField]
		Component component;

		T cache;

		[NonSerialized]
		bool cached;

		public T Interface
        {
			get
            {
				if (cached == false)
				{
					cache = component as T;
					cached = true;
				}

				return cache;
			}
        }

		public override string ToString() => Interface?.ToString();

		public InterfaceComponentSelection(Component component)
        {
			this.component = component;
		}
	}
}