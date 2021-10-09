using System;
using UnityEngine;

using System.Linq;
using System.Collections.Generic;
using System.Reflection;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditorInternal;
using UnityEditor.Build.Reporting;
#endif

using Object = UnityEngine.Object;
using Debug = UnityEngine.Debug;

namespace MB
{
	/// <summary>
	/// A Singleton ScriptableObject backed settings entry that can be edited in the Projects Settings window
	/// and accessed during run-time
	/// </summary>
	public class ScriptableManager : ScriptableObject
	{
		/// <summary>
		/// Dynamically evaluated property to determine whether to include this manager in build or not
		/// </summary>
		protected virtual bool IncludeInBuild => true;
		
		protected virtual void OnEnable()
		{
			
		}

		/// <summary>
		/// Load method invoked when the Scriptable Manager is loaded in memory
		/// </summary>
		protected virtual void Load()
		{
			
		}

		//Static Utility
		
		public static string FormatID(Type type)
		{
			return type.Name;
		}
		
		public static void Log(object target)
		{
			if (Application.isEditor)
				Debug.LogWarning(target);
			else
				Debug.LogError(target);
		}
		
		#if UNITY_EDITOR
		/// <summary>
		/// Manually Loads an instance whenever the runtime is initialized
		/// </summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void OnLoad()
		{
			//Manually Load all Scriptable Settings on Runtime
			foreach (var type in IterateAll())
				IO.Retrieve(type);
		}

		protected static IEnumerable<Type> IterateAll()
		{
			var types = TypeCache.GetTypesWithAttribute<GlobalAttribute>();

			for (var i = 0; i < types.Count; i++)
			{
				if (typeof(ScriptableManager).IsAssignableFrom(types[i]) == false)
				{
					Debug.LogWarning($"Type {types[i]} Needs to Inherit from {typeof(ScriptableManager)} to Accept the Attribute");
					continue;
				}
				
				yield return types[i];
			}
		}
		
		protected static class IO
		{
			private static readonly Dictionary<Type, ScriptableManager> dictionary;

			public static string FormatPath(Type type)
			{
				var name = FormatID(type);
				
				var attribute = GlobalAttribute.Retrieve(type);
				var scope = ConvertScope(attribute.Scope);
				
				var directory = FormatPathDirectory(scope);
				Directory.CreateDirectory(directory);
				
				return Path.Combine(directory, $"{name}.asset");
			}

			public static string FormatPathDirectory(SettingsScope scope)
			{
				switch (scope)
				{
					case SettingsScope.Project:
						return "ProjectSettings/MB/";

					case SettingsScope.User:
						var parent = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
						return Path.Combine(parent, $"Unity/Editor-5.x/Preferences/MB/{InternalEditorUtility.GetUnityDisplayVersion()}/");
				}

				throw new NotImplementedException();
			}
			
			/// <summary>
			/// Saves a settings object to disk
			/// </summary>
			/// <param name="asset"></param>
			public static void Save(ScriptableManager asset)
			{
				var type = asset.GetType();
				var path = FormatPath(type);

				dictionary[type] = asset;
				
				var array = new Object[] { asset };
				InternalEditorUtility.SaveToSerializedFileAndForget(array, path, true);
			}
			
			/// <summary>
			/// Loads a settings object from disk or from memory if already loaded,
			/// guaranteed to return the same instance within the same assembly reload
			/// </summary>
			/// <param name="type"></param>
			/// <returns></returns>
			public static ScriptableManager Load(Type type)
			{
				if (dictionary.TryGetValue(type, out var asset) && asset != null)
					return asset;

				var path = FormatPath(type);
				
				asset = InternalEditorUtility.LoadSerializedFileAndForget(path).FirstOrDefault() as ScriptableManager;
				if (asset == null) return null;

				dictionary[type] = asset;
				Setup(asset);
				
				return asset;
			}

			/// <summary>
			/// Creates a settings object instance and saves it
			/// </summary>
			/// <param name="type"></param>
			/// <returns></returns>
			public static ScriptableManager Create(Type type)
			{
				var asset = CreateInstance(type) as ScriptableManager;

				asset.name = FormatID(type);
				
				dictionary[type] = asset;
				Setup(asset);
				Save(asset);

				return asset;
			}

			/// <summary>
			/// Retrieves an instance of a settings object whether by loading it or creating
			/// </summary>
			/// <param name="type"></param>
			/// <returns></returns>
			public static ScriptableManager Retrieve(Type type)
			{
				var asset = Load(type);

				if (asset == null) asset = Create(type);

				return asset;
			}

			private static void Setup(ScriptableManager asset)
			{
				asset.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontUnloadUnusedAsset;
			}
			
			private static void PreAssemblyReloadCallback()
			{
				//Destroy assets before we lose reference to it
				foreach (var asset in dictionary.Values)
					DestroyImmediate(asset);

				dictionary.Clear();
			}
			
			static IO()
			{
				dictionary = new Dictionary<Type, ScriptableManager>();
				AssemblyReloadEvents.beforeAssemblyReload += PreAssemblyReloadCallback;
			}
		}
		
		public class Provider : SettingsProvider
		{
			private readonly Type type;
			
			private ScriptableManager asset;
			private Editor inspector;

			private void Validate()
			{
				asset = IO.Retrieve(type);
				
				if (asset == null)
				{
					
				}
				else
				{
					if (inspector == null || inspector.target != asset || inspector.target == null)
					{
						inspector = Editor.CreateEditor(asset);
					}
				}
			}

			public override void OnGUI(string search)
			{
				base.OnGUI(search);

				Validate();

				if (asset == null)
				{
					EditorGUILayout.HelpBox("No Asset Loaded in Memory", MessageType.Error);
				}
				else
				{
					EditorGUI.BeginChangeCheck();
					inspector.OnInspectorGUI();
					if (EditorGUI.EndChangeCheck())
					{
						IO.Save(asset);
					}
				}
			}

			public Provider(string path, SettingsScope scope, Type type) : base(path, scope)
			{
				this.type = type;
			}

			//Static Utility
			
			[SettingsProviderGroup]
			private static SettingsProvider[] Register()
			{
				var list = new List<SettingsProvider>();

				foreach (var type in IterateAll())
				{
					var global = GlobalAttribute.Retrieve(type);
					
					var menu = SettingsMenuAttribute.Retrieve(type);
					if(menu == null) continue;

					var provider = Create(type, global, menu);
					list.Add(provider);
				}

				return list.ToArray();
			}
			
			public static Provider Create(Type type, GlobalAttribute global, SettingsMenuAttribute menu)
			{
				var scope = ConvertScope(global.Scope);
				var path = menu.Root ? menu.Path : PrefixPath(menu.Path, scope);

				return new Provider(path, scope, type);
			}

			public static string PrefixPath(string path, SettingsScope scope)
			{
				switch (scope)
				{
					case SettingsScope.Project:
						return $"Project/{path}";
					
					case SettingsScope.User:
						return $"Preferences/{path}";
				}

				throw new NotImplementedException();
			}
		}
		
		public class BuildPreProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
		{
			public int callbackOrder => 0;

			private static string FormatFilePath(Type type)
			{
				var name = FormatID(type);

				return $"Assets/{name}.asset";
			}
			
			public void OnPreprocessBuild(BuildReport report)
			{
				using (PreloadedAssets.Lease(out var set))
				{
					foreach (var type in IterateAll())
					{
						if(EditorOnlyAttribute.IsDefined(type))
							continue;
						
						var path = FormatFilePath(type);
					
						AssetDatabase.DeleteAsset(path);
					
						var asset = IO.Retrieve(type);
						if(asset.IncludeInBuild == false) continue;
						
						asset.hideFlags = HideFlags.None;
						AssetDatabase.CreateAsset(asset, path);

						set.Add(asset);
					}
				}
			}
			
			public void OnPostprocessBuild(BuildReport report)
			{
				using (PreloadedAssets.Lease(out var set))
				{
					foreach (var type in IterateAll())
					{
						if(EditorOnlyAttribute.IsDefined(type))
							continue;
						
						var path = FormatFilePath(type);
						var asset = AssetDatabase.LoadAssetAtPath<ScriptableManager>(path);
					
						set.Remove(asset);
						AssetDatabase.DeleteAsset(path);
					}
				}
			}
		}

		public static SettingsScope ConvertScope(ScriptableManagerScope scope)
		{
			switch (scope)
			{
				case ScriptableManagerScope.Project:
					return SettingsScope.Project;
				
				case ScriptableManagerScope.User:
					return SettingsScope.User;
			}

			throw new NotImplementedException();
		}
		public static ScriptableManagerScope ConvertScope(SettingsScope scope)
		{
			switch (scope)
			{
				case SettingsScope.Project:
					return ScriptableManagerScope.Project;
				
				case SettingsScope.User:
					return ScriptableManagerScope.User;
			}

			throw new NotImplementedException();
		}
		#endif
		
		#region Attributes
		/// <summary>
		/// Attribute that needs to be applied to all Scriptable Manager instances for them to register in the system
		/// </summary>
		[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
		public sealed class GlobalAttribute : Attribute
		{
			public ScriptableManagerScope Scope { get; }
		
			public GlobalAttribute(ScriptableManagerScope scope)
			{
				this.Scope = scope;
			}

			public static GlobalAttribute Retrieve(Type type)
			{
				return type.GetCustomAttribute<GlobalAttribute>();
			}
		}
		
		/// <summary>
		/// Shows the Scriptable Manager in the appropriate Unity settings menu,
		/// Projects Settings Menu for Project scoped Managers,
		/// Preferences Menu for User scoped Managers.
		/// </summary>
		[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
		public sealed class SettingsMenuAttribute : Attribute
		{
			public string Path { get; }
			public bool Root { get; }

			public SettingsMenuAttribute(string path) : this(path, false) {}
			public SettingsMenuAttribute(string path, bool root)
			{
				this.Path = path;
				this.Root = root;
			}

			public static SettingsMenuAttribute Retrieve(Type type)
			{
				return type.GetCustomAttribute<SettingsMenuAttribute>();
			}
		}
		
		/// <summary>
		/// Flag attribute that tells the system that this manager is to be used in editor only
		/// </summary>
		[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
		public sealed class EditorOnlyAttribute : Attribute
		{
			public static bool IsDefined(Type type)
			{
				return type.GetCustomAttribute<EditorOnlyAttribute>() != null;
			}
		}
		#endregion
	}
	
	public class ScriptableManager<T> : ScriptableManager
		where T : ScriptableManager<T>
	{
		private static T instance;
		public static T Instance
		{
			get
			{
				#if UNITY_EDITOR
				if (instance == null)
				{
					var type = typeof(T);
					instance = IO.Retrieve(type) as T;
				}
				#endif

				return instance;
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			
			instance = this as T;
			
			Load();
		}
	}

	public enum ScriptableManagerScope
	{
		/// <summary>
		/// Saved for User and Applicable to all Projects
		/// </summary>
		User,
		
		/// <summary>
		/// Saved for Current Project
		/// </summary>
		Project,
	}
}