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
using MB.ThirdParty;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

using System.Text;
using System.Reflection;

namespace MB
{
	/// <summary>
	/// Provides a Unity serializable monoscript backed type field (can whistand name changes & moving),
	/// can be restricted to use specific derived types using the nested SelectionAttribute
	/// </summary>
	[Serializable]
	public class SerializedMonoScript : ISerializationCallbackReceiver
	{
		[SerializeField]
		Object asset = default;
		public Object Asset => asset;

		[SerializeField]
		string id = default;
		public string ID => id;

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
						cache = Type.GetType(id);

					cached = true;
				}

				return cache;
			}
		}

		public void OnBeforeSerialize()
		{
#if UNITY_EDITOR
			cached = false;

			if (asset == null)
			{
				id = string.Empty;
				return;
			}

			var type = (asset as MonoScript).GetClass();
			if (type == null)
			{
				id = string.Empty;
				return;
			}

			id = type.AssemblyQualifiedName;
#endif
		}
		public void OnAfterDeserialize()
        {
			cached = false;
        }

		public override string ToString()
		{
			if (Type == null)
				return "null";

			return Type.ToString();
		}

		public static implicit operator Type(SerializedMonoScript target) => target.Type;

#if UNITY_EDITOR
		[CustomPropertyDrawer(typeof(SerializedMonoScript), true)]
		[CustomPropertyDrawer(typeof(SelectionAttribute), true)]
		public class Drawer : PropertyDrawer
		{
			void GetMetadata(out Type argument, out Includes includes)
            {
				var attribute = base.attribute as SelectionAttribute;

				if (attribute == null)
				{
					argument = typeof(MonoBehaviour);
					includes = Defaults.Include;
				}
				else
				{
					argument = attribute.Argument;
					includes = attribute.Includes;
				}
			}

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
				return EditorGUIUtility.singleLineHeight;
            }

			public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
			{
				GetMetadata(out var argument, out var includes);

				var asset = property.FindPropertyRelative("asset");
				var selection = asset.objectReferenceValue as MonoScript;

				if(ValidateType(argument, selection, includes))
                {
					rect = EditorGUI.PrefixLabel(rect, label);

					var content = new GUIContent(FormatDisplayName(selection));

					if (EditorGUI.DropdownButton(rect, content, FocusType.Keyboard))
					{
						var scripts = Query(argument, includes);
						var names = scripts.Select(FormatDisplayName);

						var index = selection == null ? -1 : scripts.IndexOf(selection);

						SearchablePopup.Show(rect, index, names, OnSelect, includeNone: true);
						void OnSelect(int index)
						{
							asset.LateModifyProperty(Process);
							void Process(SerializedProperty property)
							{
								if (index == -1)
									property.objectReferenceValue = null;
								else
									property.objectReferenceValue = scripts[index];
							}
						}
					}
				}
				else
                {
					var area = MUtility.GUICoordinates.SliceHorizontalPercentage(ref rect, 80f);
					EditorGUI.HelpBox(area, $" Invalid Selection of {FormatDisplayName(selection)}", MessageType.Error);

					rect.xMin += 2;

					if (GUI.Button(rect, "Clear"))
						asset.objectReferenceValue = null;
				}
			}

			public static string FormatDisplayName(MonoScript script)
			{
				if (script == null)
					return "None";

				var type = script.GetClass();
				return type.FullName;
			}

			public static bool ValidateType(Type argument, MonoScript script, Includes includes)
            {
				if (script == null) return true;

				var type = script.GetClass();
				if (type == null) return false;

				if (includes.HasFlag(Includes.Self) == false && type == argument) return false;
				if (includes.HasFlag(Includes.Abstract) == false && type.IsAbstract) return false;
				if (includes.HasFlag(Includes.Generic) == false && type.IsGenericType) return false;

				if (argument.IsAssignableFrom(type) == false) return false;
				if (IgnoreAttribute.IsDefined(type)) return false;

				return true;
			}

			public static IList<MonoScript> Query(Type argument, Includes includes)
			{
				var list = new List<MonoScript>();

				for (int i = 0; i < AssetCollection.List.Count; i++)
				{
					if (AssetCollection.List[i] is MonoScript script)
					{
						if (ValidateType(argument, script, includes))
							list.Add(script);
					}
				}

				return list;
			}
		}
#endif

		[Flags]
		public enum Includes
		{
			None = 0,

			Self = 1 << 0,
			Abstract = 1 << 1,
			Generic = 1 << 2,

			All = Self | Abstract | Generic,
		}

		public static class Defaults
		{
			public const Includes Include = Includes.None;
		}

		[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
		public class SelectionAttribute : PropertyAttribute
		{
			public Type Argument { get; }
			public Includes Includes { get; set; } = Defaults.Include;

			public SelectionAttribute(Type argument)
			{
				this.Argument = argument;
			}
		}

		/// <summary>
		/// Attribute that ignores the specified class from selection
		/// </summary>
		[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
		public sealed class IgnoreAttribute : Attribute
		{
#if UNITY_EDITOR
			public static bool IsDefined(MonoScript script)
			{
				var type = script.GetClass();

				return IsDefined(type);
			}
			public static bool IsDefined(Type type)
			{
				var attribute = type.GetCustomAttribute<IgnoreAttribute>();

				return attribute != null;
			}
#endif
		}
	}
}