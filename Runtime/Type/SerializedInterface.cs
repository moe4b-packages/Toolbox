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
using System.Net.Mime;

namespace MB
{
	/// <summary>
	/// A serializable field that allows you to select a component that implements a specific interface
	/// </summary>
	[Serializable]
	public class SerializedInterface
	{
		[SerializeField]
		protected Object context;

		[SerializeField]
		protected Object target;

#if UNITY_EDITOR
		[CustomPropertyDrawer(typeof(SerializedInterface), true)]
		public class Drawer : PropertyDrawer
		{
			SerializedProperty FindContextProperty(SerializedProperty property) => property.FindPropertyRelative("context");
			SerializedProperty FindTargetProperty(SerializedProperty property) => property.FindPropertyRelative("target");

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
				return EditorGUIUtility.singleLineHeight;
			}

			public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
            {
				var type = property.MakeSmart().ManagedType.GenericTypeArguments[0];

				var context = FindContextProperty(property);
				var target = FindTargetProperty(property);

				var areas = MUtility.GUICoordinates.SplitHorizontally(rect, 5, 70f, 30f);

				EditorGUI.BeginChangeCheck();
				{
					DrawContext(areas[0], label, context);
				}
				if (EditorGUI.EndChangeCheck())
					UpdateState(context, target, type);

				DrawTarget(areas[1], context, target, type);
			}

			void UpdateState(SerializedProperty context, SerializedProperty target, Type type)
			{
				if (context.objectReferenceValue is GameObject gameObejct)
					target.objectReferenceValue = gameObejct.GetComponent(type);
				else if (type.IsAssignableFrom(context.objectReferenceValue))
					target.objectReferenceValue = context.objectReferenceValue;
				else
					target.objectReferenceValue = null;
			}

			void DrawContext(Rect rect, GUIContent label, SerializedProperty context)
			{
				rect.x -= 2.5f;

				EditorGUI.PropertyField(rect, context, label);
			}

			void DrawTarget(Rect rect, SerializedProperty context, SerializedProperty target, Type type)
			{
				var label = FormatTargetName(target.objectReferenceValue);
				var content = new GUIContent(label);

				if (EditorGUI.DropdownButton(rect, content, FocusType.Passive))
				{
					Dropdown.Show(context.objectReferenceValue, target.objectReferenceValue, type, rect, Handler);

					void Handler(Object value)
                    {
						target.LateModifyProperty(x => x.objectReferenceValue = value);
					}
				}
			}
		}

		public static class Dropdown
        {
			public delegate void HandlerDelegate(Object target);
			public static void Show(Object context, Object target, Type type, Rect rect, HandlerDelegate handler)
			{
				var menu = new GenericMenu();

				if(type.IsAssignableFrom(context))
                {
					var label = FormatTargetName(context);
					var content = new GUIContent(label);

					menu.AddItem(content, context == target, Callback, context);
				}

				if (context is GameObject gameObject)
				{
					var components = gameObject.GetComponents(type);

					for (int i = 0; i < components.Length; i++)
					{
						var label = FormatTargetName(components[i]);
						var content = new GUIContent(label);

						menu.AddItem(content, components[i] == target, Callback, components[i]);
					}
				}

				menu.AddSeparator("");

				menu.AddItem(new GUIContent("None"), target == null, Callback, null);

				void Callback(object target)
                {
					var component = target as Object;
					handler(component);
                }

				menu.DropDown(rect);
			}
		}

		public static string FormatTargetName(Object target)
        {
			if (target == null) return "None";

			return MUtility.PrettifyName(target.GetType().Name);
		}
#endif
	}

	[Serializable]
	public class SerializedInterface<T> : SerializedInterface
		where T : class 
	{
		T cache;

		[NonSerialized]
		bool cached;

		public T Interface
        {
			get
            {
				if (cached == false)
				{
					cache = target as T;
					cached = true;
				}

				return cache;
			}
        }

		public override string ToString() => Interface?.ToString();

		protected void Set(Object target)
        {
			this.target = target;

			if (target is Component component)
				context = component.gameObject;
			else
				context = target;
        }

		public SerializedInterface(Object target)
        {
			Set(target);
		}
		public SerializedInterface(T target)
		{
			if (target is Object obj)
				Set(obj);
			else
				throw new InvalidOperationException($"Can't Assign '{target}' Interface to SerializedInterface Because it's not a Unity Object");
		}
	}
}