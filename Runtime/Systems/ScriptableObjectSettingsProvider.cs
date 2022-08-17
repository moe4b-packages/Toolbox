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

using UnityEngine.UIElements;
using System.Reflection;

namespace MB
{
	/// <summary>
	/// Attribute that allows showing a certain type of Scriptable Obejcts in the project settings menu,
	/// will look for the first instance of the Scriptable Object and draw an error UI if no instance is found
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class ScriptableObjectSettingsProviderAttribute : Attribute
	{
		public string Path { get; }

		public ScriptableObjectSettingsProviderAttribute(string path)
		{
			this.Path = path;
		}
	}
	
#if UNITY_EDITOR
	public class ScriptableObjectSettingsProvider
	{
		public class Provider : SettingsProvider
		{
			public Type Type { get; }
			public ScriptableObjectSettingsProviderAttribute Attribute { get; }

			public ScriptableObject Target { get; private set; }
			public Editor Inspector;

			protected virtual void ValidateTarget()
			{
				if (Target != null) return;

				Target = AssetCollection.Find<ScriptableObject>(Predicate);
				bool Predicate(ScriptableObject asset) => Type.IsAssignableFrom(asset);

				if (Target == null) return;

				Inspector = Editor.CreateEditor(Target);
			}

			public override void OnActivate(string search, VisualElement root)
			{
				base.OnActivate(search, root);

				ValidateTarget();
			}

			public override void OnGUI(string search)
			{
				base.OnGUI(search);

				ValidateTarget();

				if (Target == null)
                {
					EditorGUILayout.HelpBox($"No {Type.Name} ScriptableObject Found", MessageType.Error);

					if (GUILayout.Button("Create"))
					{
						var asset = ScriptableObject.CreateInstance(Type);
						var name = MUtility.Text.Prettify(Type.Name);
						ProjectWindowUtil.CreateAsset(asset, $"Assets/{name}.asset");
					}
				}
				else
                {
					Inspector.OnInspectorGUI();
				}
			}

			public Provider(Type type, ScriptableObjectSettingsProviderAttribute attribute, string path) : base(path, SettingsScope.Project)
			{
				this.Type = type;
				this.Attribute = attribute;
			}
		}

		[SettingsProviderGroup]
		static SettingsProvider[] Register()
		{
			var list = new List<SettingsProvider>();

			var types = TypeCache.GetTypesWithAttribute<ScriptableObjectSettingsProviderAttribute>();

			for (int i = 0; i < types.Count; i++)
			{
				var attribute = types[i].GetCustomAttribute<ScriptableObjectSettingsProviderAttribute>();

				var provider = new Provider(types[i], attribute, attribute.Path);

				list.Add(provider);
			}

			return list.ToArray();
		}
	}
#endif
}