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
		public class Drawer : PersistantPropertyDrawer
		{
			SerializedProperty gameObject;
			SerializedProperty component;

			Type type;

			Component[] options;
			string[] popup;
			int selection;

            protected override void Init()
            {
                base.Init();

				gameObject = property.FindPropertyRelative(nameof(gameObject));
				component = property.FindPropertyRelative(nameof(component));

				type = MUtility.SerializedPropertyType.Retrieve(property).GenericTypeArguments[0];

				UpdateComponents();
			}

            protected override float CalculateHeight()
            {
				return EditorGUIUtility.singleLineHeight;
			}

            protected override void Draw(Rect rect)
            {
				var areas = MUtility.GUICoordinates.SplitHorizontally(rect, 5, 70f, 30f);

				DrawGameObject(areas[0], label);
				DrawComponent(areas[1]);
			}

            void DrawGameObject(Rect rect, GUIContent label)
			{
				rect.x -= 2.5f;

				EditorGUI.BeginChangeCheck();
				{
					EditorGUI.PropertyField(rect, gameObject, label);
				}
				if (EditorGUI.EndChangeCheck())
					UpdateComponents();
			}

			void UpdateComponents()
            {
				var target = gameObject.objectReferenceValue as GameObject;

				if (target == null)
					options = new Component[] { };
				else
					options = target.GetComponents(type);

				popup = new string[options.Length + 1];

				popup[0] = "None";
				selection = 0;

				for (int i = 0; i < options.Length; i++)
				{
					if (component.objectReferenceValue == options[i]) selection = i + 1;

					popup[i + 1] = $"{i + 1}. {options[i].GetType().Name.ToDisplayString()}";
				}

				if (selection == 0 && options.Length > 0) selection = 1;
			}

			void DrawComponent(Rect rect)
			{
				selection = EditorGUI.Popup(rect, selection, popup);

				if (selection == 0)
					component.objectReferenceValue = null;
				else
					component.objectReferenceValue = options[selection - 1];
			}
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