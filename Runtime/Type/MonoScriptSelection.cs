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

using System.Text;
using System.Reflection;

namespace MB
{
	/// <summary>
	/// A serializable field that allows you to select a monoscript file that implmenents a certain type
	/// </summary>
	[Serializable]
	public abstract class MonoScriptSelection : ISerializationCallbackReceiver
	{
		[SerializeField]
		Object asset = default;
		public Object Asset => asset;

		[SerializeField]
		string id = default;
		public string ID => id;

		public abstract Type Argument { get; }

		bool cached = default;
		Type cache = default;

		public Type Type
		{
			get
			{
				if (cached == false)
				{
					if (string.IsNullOrEmpty(id))
						cache = null;
					else
					{
						cache = Type.GetType(id);

						if (Argument.IsAssignableFrom(cache) == false)
							cache = null;
					}

					cached = true;
				}

				return cache;
			}
			set
			{
				id = Type.AssemblyQualifiedName;
			}
		}

#if UNITY_EDITOR
		void Refresh()
		{
			if (asset == null)
			{
				id = string.Empty;
				return;
			}

			var type = (asset as MonoScript).GetClass();
			id = type?.AssemblyQualifiedName;
		}
#endif

		public void OnAfterDeserialize() { }
		public void OnBeforeSerialize()
		{
#if UNITY_EDITOR
			Refresh();
#endif
		}

		public override string ToString()
		{
			if (Type == null)
				return "null";

			return Type.ToString();
		}

#if UNITY_EDITOR
		[CustomPropertyDrawer(typeof(MonoScriptSelection), true)]
		public class Drawer : PropertyDrawer
		{
			SerializedProperty FindAssetProperty(SerializedProperty property) => property.FindPropertyRelative("asset");

            //protected override void Init()
            //{
            //    base.Init();
			//
			//	var list = AssetQuery<MonoScript>.FindAll(IsScriptOfType);
			//	list.Sort(SortMonoScripts);
			//
			//	Popup = new PopupDrawer<MonoScript>(label);
			//	Popup.Populate(list, FormatMonoScriptName, Asset.objectReferenceValue as MonoScript);
			//	Popup.OnSelect += SelectCallback;
			//}

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
				return EditorGUIUtility.singleLineHeight;
            }

            public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
            {
				var argument = property.MakeSmart().ManagedType.GenericTypeArguments[0];

				var asset = FindAssetProperty(property);
				var script = asset.objectReferenceValue as MonoScript;

				if (ValidateScript(script, argument) == false)
				{
					DrawError(rect, asset);
					return;
				}

				DrawLabel(ref rect, label);
				DrawDropdown(ref rect, asset, argument);
			}

			void DrawLabel(ref Rect rect, GUIContent label)
			{
				rect = EditorGUI.PrefixLabel(rect, label);
			}

			void DrawDropdown(ref Rect rect, SerializedProperty asset, Type argument)
            {
				var area = MUtility.GUICoordinates.SliceLine(ref rect);

				var label = FormatMonoScriptName(asset.objectReferenceValue as MonoScript);
				var content = new GUIContent(label);

				if (EditorGUI.DropdownButton(area, content, FocusType.Passive))
				{
					Dropdown.Show(argument, area, Handler);

					void Handler(MonoScript selection)
					{
						asset.objectReferenceValue = selection;
						asset.serializedObject.ApplyModifiedProperties();
					}
				}
			}

			bool ValidateScript(MonoScript script, Type argument)
			{
				if (script == null) return true;

				return IsScriptOfType(script, argument);
			}

			void DrawError(Rect rect, SerializedProperty asset)
			{
				var width = 80f;
				var spacing = 5f;

				rect.width -= width;

				var name = asset.objectReferenceValue.name.ToPrettyString();
				EditorGUI.HelpBox(rect, $"Type '{name}' Invalid", MessageType.Error);

				rect.x += rect.width + spacing;
				rect.width = width - spacing;

				if (GUI.Button(rect, "Clear"))
					asset.objectReferenceValue = null;
			}
		}

		public static class Dropdown
        {
			static Dictionary<Type, Entry> Dictionary;
			public class Entry
			{
				GenericMenu Menu;

				public void Show(Rect position, HandlerDelegate handler)
				{
					OnCallback = handler;
					//Menu.ShowAsContext();
					Menu.DropDown(position);
				}

				HandlerDelegate OnCallback;
				void Callback(object target)
                {
					var script = target as MonoScript;

					OnCallback.Invoke(script);
				}

				public Entry(Type argument)
				{
					var scripts = AssetCollection.Query<MonoScript>(Predicate);
					bool Predicate(MonoScript script)
					{
						if (IsScriptOfType(script, argument) == false)
							return false;

						if (NoneMonoScriptSelectableAttribute.IsDefined(script))
							return false;

						return true;
					}

					Menu = new GenericMenu();

					Menu.AddItem(new GUIContent("None"), false, Callback, null);

					for (int i = 0; i < scripts.Count; i++)
                    {
						var label = FormatMonoScriptName(scripts[i]);
						var content = new GUIContent(label);

						Menu.AddItem(content, false, Callback, scripts[i]);
					}
				}
			}

			public delegate void HandlerDelegate(MonoScript script);

			public static void Show(Type type, Rect rect, HandlerDelegate handler)
			{
				if(Dictionary.TryGetValue(type, out var entry) == false)
                {
					entry = new Entry(type);
					Dictionary[type] = entry;
                }

				entry.Show(rect, handler);
			}

			static Dropdown()
            {
				Dictionary = new Dictionary<Type, Entry>();
			}
		}

		public static bool IsScriptOfType(MonoScript script, Type argument)
		{
			var type = script.GetClass();

			if (type == argument)
				return false;

			if (argument.IsAssignableFrom(type) == false)
				return false;

			return true;
		}

		public static int SortMonoScripts(MonoScript right, MonoScript left)
		{
			return right.name.CompareTo(left.name);
		}

		public static string FormatMonoScriptName(MonoScript script)
		{
			if (script == null) return "None";

			return MUtility.PrettifyName(script.name);
		}
#endif
	}

	[Serializable]
	public class MonoScriptSelection<T> : MonoScriptSelection
		where T : class
	{
		public override Type Argument => typeof(T);
	}

	/// <summary>
	/// Attribute that specifies that this specific Monobehaviour derived class
	/// should not be a selectable option with a MonoScriptSelection field
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class NoneMonoScriptSelectableAttribute : Attribute
	{
#if UNITY_EDITOR
		public static bool IsDefined(MonoScript script)
		{
			var type = script.GetClass();

			return IsDefined(type);
		}
#endif
		public static bool IsDefined(Type type)
		{
			var attribute = type.GetCustomAttribute<NoneMonoScriptSelectableAttribute>();

			return attribute != null;
		}
	}
}