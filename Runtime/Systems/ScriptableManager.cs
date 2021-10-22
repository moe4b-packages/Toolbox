using System;
using UnityEngine;

using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.UIElements;

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
	public abstract class ScriptableManager : ScriptableObject
	{
		protected virtual void Awake()
		{
			if (Application.isPlaying) Debug.LogError($"Loaded {this}");

#if UNITY_EDITOR == false
			OnLoad();
#endif
		}

#if UNITY_EDITOR
		internal void InternalInvoke_OnLoad() => OnLoad();

		internal void InternalInvoke_PreProcessBuild() => PreProcessBuild();
		protected virtual void PreProcessBuild() { }
#endif

		/// <summary>
		/// Load method invoked when the Scriptable Manager is loaded in memory
		/// </summary>
		protected virtual void OnLoad()
		{

		}

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

#if UNITY_EDITOR
			public static bool IsDefined(Type type)
			{
				return type.GetCustomAttribute<GlobalAttribute>() != null;
			}
#endif
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

			public SettingsMenuAttribute(string path) : this(path, false) { }
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
		/// Makes the Scriptable Manager's project settings window read only in the defined play mode
		/// </summary>
		[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
		public sealed class ReadOnlySettingsAttribute : MB.ReadOnlyAttribute
		{
			public ReadOnlySettingsAttribute() : base()
			{

			}
			public ReadOnlySettingsAttribute(ReadOnlyPlayMode mode) : base(mode)
			{
			}

			public static ReadOnlySettingsAttribute Retrieve(Type type)
			{
				return type.GetCustomAttribute<ReadOnlySettingsAttribute>();
			}

			public static ReadOnlyPlayMode ReadMode(Type type)
			{
				var attribute = Retrieve(type);

				if (attribute == null)
					return ReadOnlyPlayMode.None;

				return attribute.Mode;
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

		/// <summary>
		/// Attribute that specifies the load order of managers
		/// </summary>
		[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
		public sealed class LoadOrderAttribute : Attribute
		{
			public int Order { get; }

			public LoadOrderAttribute(int order)
			{
				this.Order = order;
			}

#if UNITY_EDITOR
			public static int GetOrder(Type type)
			{
				var attribute = type.GetCustomAttribute<LoadOrderAttribute>();

				if (attribute == null)
					return 0;

				return attribute.Order;
			}
#endif
		}
		#endregion

#if UNITY_EDITOR
		[CustomEditor(typeof(ScriptableManager), true)]
		protected class BaseInspector : Editor
		{
			public override void OnInspectorGUI()
			{
				DrawPropertiesExcluding(serializedObject, "m_Script");
				serializedObject.ApplyModifiedProperties();
			}
		}
#endif
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
					instance = ScriptableManagerRuntime.Retrieve(type) as T;
				}
#endif

				return instance;
			}
		}

		protected override void Awake()
		{
			instance = this as T;

			base.Awake();
		}
	}

#if UNITY_EDITOR
	/// <summary>
	/// Class Responsible for all Scriptable Manager operations,
	/// seperated into it's own class instead of begin nested in Scriptable Manager to reduce inheritance conflicts,
	/// ironically despite its name, it's an editor only class
	/// </summary>
	public static class ScriptableManagerRuntime
	{
		internal static readonly Dictionary<Type, ScriptableManager> dictionary;

		/// <summary>
		/// Manually Loads all managers
		/// </summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		public static void LoadAll()
		{
			foreach (var type in IterateAll())
				Retrieve(type);
		}

		static void Validate(ScriptableManager manager)
		{
			if (manager == null) throw new ArgumentNullException(nameof(manager));

			var type = manager.GetType();

			if (dictionary.ContainsKey(type) == false)
				throw new InvalidOperationException($"Invalid Manager Argument for {type} Passed");

			if (dictionary[type] != manager)
				throw new InvalidOperationException($"Invalid Manager Argument for {type} Passed");
		}
		static void Assign(ScriptableManager manager)
		{
			if (manager == null) throw new ArgumentNullException(nameof(manager));

			var type = manager.GetType();
			dictionary.Add(type, manager);

			manager.InternalInvoke_OnLoad();
		}

		public static ScriptableManager Retrieve(Type type)
		{
			if (dictionary.TryGetValue(type, out var manager) && manager != null)
				return manager;

			manager = IO.Load(type);

			if (manager == null)
				manager = IO.Create(type);

			Assign(manager);

			return manager;
		}
		public static ScriptableManager Reset(Type type)
		{
			Destroy(type);

			var manager = IO.Create(type);
			Assign(manager);

			return manager;
		}
		public static ScriptableManager Reload(Type type)
		{
			Destroy(type);
			return Retrieve(type);
		}

		public static void Save(ScriptableManager manager)
		{
			if (manager == null) throw new ArgumentNullException(nameof(manager));
			Validate(manager);

			IO.Save(manager);
		}

		internal static bool Destroy(Type type)
		{
			if (dictionary.TryGetValue(type, out var manager) == false)
				return false;

			return Destroy(manager);
		}
		internal static bool Destroy(ScriptableManager manager)
		{
			if (manager == null) throw new ArgumentNullException(nameof(manager));
			Validate(manager);

			var type = manager.GetType();

			if (AssetDatabase.Contains(manager))
			{
				var path = AssetDatabase.GetAssetPath(manager);
				dictionary.Remove(type);
				return AssetDatabase.DeleteAsset(path);
			}
			else
			{
				dictionary.Remove(type);

				Object.DestroyImmediate(manager);
				return true;
			}
		}
		public static void DestroyAll()
		{
			var collection = dictionary.Values.ToArray();

			foreach (var manager in collection)
				Destroy(manager);

			dictionary.Clear();
		}

		internal static class IO
		{
			internal static string FormatPath(Type type)
			{
				var name = FormatID(type);

				var attribute = ScriptableManager.GlobalAttribute.Retrieve(type);
				var scope = ConvertScope(attribute.Scope);

				var directory = FormatDirectory(scope);
				Directory.CreateDirectory(directory);

				return Path.Combine(directory, $"{name}.asset");

				static string FormatDirectory(SettingsScope scope)
				{
					switch (scope)
					{
						case SettingsScope.Project:
							return "ProjectSettings/MB/";

						case SettingsScope.User:
							var parent = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
							return Path.Combine(parent, $"{InternalEditorUtility.unityPreferencesFolder}/MB/{InternalEditorUtility.GetUnityDisplayVersion()}/");
					}

					throw new NotImplementedException();
				}
			}

			internal static void Save(ScriptableManager manager)
			{
				if (manager == null) throw new ArgumentNullException(nameof(manager));

				var type = manager.GetType();
				var path = FormatPath(type);

				var array = new Object[] { manager };
				InternalEditorUtility.SaveToSerializedFileAndForget(array, path, true);
			}

			internal static ScriptableManager Load(Type type)
			{
				var path = FormatPath(type);

				var asset = InternalEditorUtility.LoadSerializedFileAndForget(path).FirstOrDefault() as ScriptableManager;
				if (asset == null) return null;

				Setup(asset);

				return asset;
			}

			internal static ScriptableManager Create(Type type)
			{
				var asset = ScriptableObject.CreateInstance(type) as ScriptableManager;

				asset.name = FormatID(type);

				Setup(asset);
				Save(asset);

				return asset;
			}

			internal static void Setup(ScriptableManager asset)
			{
				asset.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontUnloadUnusedAsset;
			}
		}

		public class Provider : SettingsProvider
		{
			private readonly Type type;

			private readonly ReadOnlyPlayMode readOnlyMode;

			private ScriptableManager manager;
			private Editor inspector;

			private readonly GenericMenu context;

			private void Validate()
			{
				manager = Retrieve(type);

				if (inspector == null || inspector.target != manager)
					inspector = Editor.CreateEditor(manager);
			}

			public override void OnTitleBarGUI()
			{
				base.OnTitleBarGUI();

				var style = (GUIStyle)"MiniPopup";
				var content = EditorGUIUtility.TrIconContent("_Popup");

				if (GUILayout.Button(content, style))
					context.ShowAsContext();
			}

			private void Reset() => ScriptableManagerRuntime.Reset(type);
			private void Reload() => ScriptableManagerRuntime.Reload(type);

			public override void OnGUI(string search)
			{
				base.OnGUI(search);

				Validate();

				GUI.enabled = !ReadOnlyAttribute.CheckPlayMode(readOnlyMode);

				EditorGUILayout.Space();

				EditorGUI.BeginChangeCheck();
				inspector.OnInspectorGUI();
				if (EditorGUI.EndChangeCheck() || EditorUtility.IsDirty(manager))
				{
					inspector.serializedObject.Update();
					Save(manager);
				}

				GUI.enabled = true;
			}

			public Provider(string path, SettingsScope scope, Type type, ReadOnlyPlayMode readOnlyMode) : base(path, scope)
			{
				this.type = type;
				this.readOnlyMode = readOnlyMode;

				//Create Generic Menu
				{
					context = new GenericMenu();

					context.AddItem("Reset", false, Reset);
					context.AddItem("Reload", false, Reload);
				}
			}

			//Static Utility
			[SettingsProviderGroup]
			private static SettingsProvider[] Register()
			{
				var list = new List<SettingsProvider>();

				foreach (var type in IterateAll())
				{
					var global = ScriptableManager.GlobalAttribute.Retrieve(type);

					var menu = ScriptableManager.SettingsMenuAttribute.Retrieve(type);
					if (menu == null) continue;

					var provider = Create(type, global, menu);
					list.Add(provider);
				}

				return list.ToArray();
			}

			public static Provider Create(Type type, ScriptableManager.GlobalAttribute global, ScriptableManager.SettingsMenuAttribute menu)
			{
				var scope = ConvertScope(global.Scope);
				var path = menu.Root ? menu.Path : PrefixPath(menu.Path, scope);

				var readOnlyMode = ScriptableManager.ReadOnlySettingsAttribute.ReadMode(type);

				return new Provider(path, scope, type, readOnlyMode);
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
			public int callbackOrder => -200;

			public const string DirectoryPath = "Assets/Scriptable Managers Cache";

			private static string FormatFilePath(Type type)
			{
				var name = FormatID(type);

				return $"{DirectoryPath}/{name}.asset";
			}

			public void OnPreprocessBuild(BuildReport report)
			{
				Directory.CreateDirectory(DirectoryPath);

				using (PreloadedAssets.Lease(out var preloaded))
				{
					foreach (var type in IterateAll())
					{
						if (ScriptableManager.EditorOnlyAttribute.IsDefined(type))
							continue;

						var path = FormatFilePath(type);

						//Just to be sure no old asset exists
						AssetDatabase.DeleteAsset(path);

						var asset = Retrieve(type);
						asset.InternalInvoke_PreProcessBuild();

						asset.hideFlags = HideFlags.None;
						AssetDatabase.CreateAsset(asset, path);

						preloaded.Add(asset);
					}
				}
			}

			public void OnPostprocessBuild(BuildReport report)
			{
				using (PreloadedAssets.Lease(out var preloaded))
				{
					foreach (var type in IterateAll())
					{
						if (ScriptableManager.EditorOnlyAttribute.IsDefined(type))
							continue;

						var path = FormatFilePath(type);
						var asset = AssetDatabase.LoadAssetAtPath<ScriptableManager>(path);

						preloaded.Remove(asset);
						Destroy(asset);
					}
				}

				Directory.Delete(DirectoryPath);
				File.Delete(DirectoryPath + ".meta");

				AssetDatabase.Refresh();
				AssetDatabase.SaveAssets();
			}
		}

		#region Utility
		public static string FormatID(Type type)
		{
			return type.Name;
		}

		public static IEnumerable<Type> IterateAll()
		{
			return TypeCache.GetTypesDerivedFrom<ScriptableManager>()
				.Where(ScriptableManager.GlobalAttribute.IsDefined)
				.OrderBy(ScriptableManager.LoadOrderAttribute.GetOrder);
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
		#endregion

		static ScriptableManagerRuntime()
		{
			dictionary = new Dictionary<Type, ScriptableManager>();

			//Destroy assets before we lose reference to it
			AssemblyReloadEvents.beforeAssemblyReload += DestroyAll;
		}
	}
#endif

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