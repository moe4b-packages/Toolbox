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
			SerializedProperty asset;

			Type argument;

			Glossary<MonoScript, int> map;

			GUIContent[] popup;

            protected override void Init()
            {
                base.Init();

				asset = Property.FindPropertyRelative(nameof(asset));

				argument = MUtility.SerializedPropertyType.Retrieve(Property).GenericTypeArguments[0];

				var list = AssetQuery<MonoScript>.FindAll(IsScriptOfArgumentType);
				list.Sort((right, left) => right.name.CompareTo(left.name));

				map = new Glossary<MonoScript, int>(list.Count);

				popup = new GUIContent[list.Count + 1];
				popup[0] = new GUIContent("None");

				for (int i = 0; i < list.Count; i++)
				{
					map.Add(list[i], i);

					var text = list[i].name.ToDisplayString();
					popup[i + 1] = new GUIContent(text);
				}
			}

			bool IsScriptOfArgumentType(MonoScript script)
			{
				var type = TypeCache.Load(script);

				if (type == argument)
					return false;

				if (argument.IsAssignableFrom(type) == false)
					return false;

				return true;
			}

			public override float CalculateHeight()
            {
				return EditorGUIUtility.singleLineHeight;
			}

			public override void Draw(Rect rect)
            {
                base.Draw(rect);

				var script = asset.objectReferenceValue as MonoScript;

				if (script == null || map.Contains(script))
					DrawPopup(rect, Label, script);
				else
					DrawError(rect);
			}

			void DrawPopup(Rect rect, GUIContent label, MonoScript script)
			{
				var index = script == null ? 0 : map[script] + 1;

				EditorGUI.BeginChangeCheck();
				{
					index = EditorGUI.Popup(rect, label, index, popup);
				}
				if (EditorGUI.EndChangeCheck())
				{
					if (index == 0)
						asset.objectReferenceValue = null;
					else
						asset.objectReferenceValue = map[index - 1];
				}
			}

			void DrawError(Rect rect)
			{
				var width = 80f;
				var spacing = 5f;

				rect.width -= width;

				EditorGUI.HelpBox(rect, $"Type '{asset.objectReferenceValue.name.ToDisplayString()}' Invalid", MessageType.Error);

				rect.x += rect.width + spacing;
				rect.width = width - spacing;

				if (GUI.Button(rect, "Clear"))
					asset.objectReferenceValue = null;
			}
		}
#endif
	}

	[Serializable]
	public class MonoScriptSelection<T> : MonoScriptSelection
	{
		public override Type Argument => typeof(T);
	}
}