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
		public static class TypeCache
		{
			public static Dictionary<MonoScript, Type> Dictionary { get; private set; }

			public static Type Load(MonoScript asset)
			{
				if (Dictionary.TryGetValue(asset, out var type) == false)
				{
					type = asset.GetClass();

					Dictionary.Add(asset, type);
				}

				return type;
			}

			static TypeCache()
			{
				Dictionary = new Dictionary<MonoScript, Type>();
			}
		}

		[CustomPropertyDrawer(typeof(MonoScriptSelection), true)]
		public class Drawer : PersistantPropertyDrawer
		{
			SerializedProperty Asset;

			Type Argument;

			public PopupDrawer<MonoScript> Popup;

            protected override void Init()
            {
                base.Init();

				Asset = Property.FindPropertyRelative("asset");

				Argument = MUtility.SerializedPropertyType.Retrieve(Property).GenericTypeArguments[0];

				var list = AssetQuery<MonoScript>.FindAll(IsScriptOfArgumentType);
				list.Sort(SortMonoScripts);

				Popup = new PopupDrawer<MonoScript>(Label);
				Popup.Populate(list, FormatMonoScriptName, Asset.objectReferenceValue as MonoScript);
				Popup.OnSelect += SelectCallback;
			}

			public override float CalculateHeight()
            {
				return EditorGUIUtility.singleLineHeight;
			}

			public override void Draw(Rect rect)
            {
				if (Popup.IsNone && Asset.objectReferenceValue != null)
					DrawError(rect);
				else
					Popup.Draw(rect);
			}

			void SelectCallback(int index, bool isNone, MonoScript selection)
			{
				Asset.objectReferenceValue = selection;
			}

			void DrawError(Rect rect)
			{
				var width = 80f;
				var spacing = 5f;

				rect.width -= width;

				EditorGUI.HelpBox(rect, $"Type '{Asset.objectReferenceValue.name.ToPrettyString()}' Invalid", MessageType.Error);

				rect.x += rect.width + spacing;
				rect.width = width - spacing;

				if (GUI.Button(rect, "Clear"))
					Asset.objectReferenceValue = null;
			}

			//Utility Methods

			bool IsScriptOfArgumentType(MonoScript script)
			{
				var type = TypeCache.Load(script);

				if (type == Argument)
					return false;

				if (Argument.IsAssignableFrom(type) == false)
					return false;

				return true;
			}
			int SortMonoScripts(MonoScript right, MonoScript left)
			{
				return right.name.CompareTo(left.name);
			}
			string FormatMonoScriptName(MonoScript script)
			{
				return MUtility.PrettifyName(script.name);
			}
		}
#endif
	}

	[Serializable]
	public class MonoScriptSelection<T> : MonoScriptSelection
		where T : class
	{
		public override Type Argument => typeof(T);
	}
}