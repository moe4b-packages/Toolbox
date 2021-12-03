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
		public static bool IsPlaying
		{
			get
			{
#if UNITY_EDITOR
				return EditorApplication.isPlayingOrWillChangePlaymode;
#else
				return true;

#endif
			}
		}

		protected virtual void Awake()
		{
#if UNITY_EDITOR == false
			Prepare();
#endif
		}

		internal abstract void Prepare();

		/// <summary>
		/// Load method invoked when the Scriptable Manager is loaded in memory,
		/// which is whenever the editor reloads assemblys, or when the player starts,
		/// evaluate IsPlaying property to check if this OnLoad was invoked when on player start
		/// </summary>
		protected virtual void OnLoad()
		{

		}

#if UNITY_EDITOR
		protected virtual void PreProcessBuild() { }

		[InitializeOnLoadMethod]
		static void OnEditorLoad()
		{
			AssemblyReloadEvents.afterAssemblyReload += AfterReload;
			static void AfterReload()
			{
				const string InitialReloadFlagID = "MB Scriptable Manager Initial Reload";

				if (SessionState.GetBool(InitialReloadFlagID, false))
				{
					Runtime.LoadAll();
				}
				else
				{
					///Delay the LoadAll command when we open the project for the first time,
					///this is to circumvent a bug where AssetDatabase operations return invalid results
					///when invoked as soon as the project starts after needing to "rebuild the asset database library"
					///such as when the Library folder is deleted
					EditorApplication.delayCall += Runtime.LoadAll;

					SessionState.SetBool(InitialReloadFlagID, true);
				}
			}
		}
#endif

		/// <summary>
		/// Class Responsible for all Scriptable Manager operations,
		/// seperated into it's own class instead of begin nested in Scriptable Manager to reduce inheritance conflicts,
		/// ironically despite its name, it's an editor only class
		/// </summary>
		public static class Runtime
		{
#if UNITY_EDITOR
			static readonly Dictionary<Type, ScriptableManager> dictionary;

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

				manager.Prepare();
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
				if (dictionary.TryGetValue(type, out var manager))
				{
					dictionary.Remove(type);

					var path = AssetDatabase.GetAssetPath(manager);
					manager = IO.Create(type, path);

					Assign(manager);
				}
				else
				{
					manager = Retrieve(type);
				}

				return manager;
			}

			public static void Save(ScriptableManager manager)
			{
				if (manager == null) throw new ArgumentNullException(nameof(manager));
				Validate(manager);

				IO.Save(manager);
			}

			internal static class IO
			{
				internal static void Save(ScriptableManager manager)
				{
					if (manager == null) throw new ArgumentNullException(nameof(manager));

					EditorUtility.SetDirty(manager);
					AssetDatabase.SaveAssetIfDirty(manager);
				}

				internal static ScriptableManager Load(Type type)
				{
					var asset = AssetCollection.Find<ScriptableManager>(type);
					return asset;
				}

				internal static ScriptableManager Create(Type type)
				{
					const string DirectoryRelativePath = "Assets/Moe Baker/Scriptable Managers/";

					var directory = new DirectoryInfo(DirectoryRelativePath);
					if (directory.Exists == false) directory.Create();

					var name = MUtility.PrettifyName(type.Name);
					var path = Path.Combine(DirectoryRelativePath, $"{name}.asset");

					return Create(type, path);
				}
				internal static ScriptableManager Create(Type type, string path)
				{
					Debug.LogWarning($"Creating {type.Name} Manager");

					var asset = CreateInstance(type) as ScriptableManager;
					AssetDatabase.CreateAsset(asset, path);

					return asset;
				}

				internal static bool Delete(ScriptableManager manager)
				{
					var path = AssetDatabase.GetAssetPath(manager);
					return AssetDatabase.DeleteAsset(path);
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

				private void Reset() => Runtime.Reset(type);

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
					}
				}

				//Static Utility
				[SettingsProviderGroup]
				private static SettingsProvider[] Register()
				{
					var list = new List<SettingsProvider>();

					foreach (var type in IterateAll())
					{
						var global = ScriptableManager.ManagerAttribute.Retrieve(type);

						var menu = ScriptableManager.SettingsMenuAttribute.Retrieve(type);
						if (menu == null) continue;

						var provider = Create(type, global, menu);
						list.Add(provider);
					}

					return list.ToArray();
				}

				public static Provider Create(Type type, ScriptableManager.ManagerAttribute global, ScriptableManager.SettingsMenuAttribute menu)
				{
					var path = menu.Root ? menu.Path : $"Project/{menu.Path}";
					var readOnlyMode = ReadOnlySettingsAttribute.ReadMode(type);

					return new Provider(path, SettingsScope.Project, type, readOnlyMode);
				}
			}

			[CustomEditor(typeof(ScriptableManager), true)]
			public class BaseInspector : Editor
			{
				public override void OnInspectorGUI()
				{
					DrawPropertiesExcluding(serializedObject, "m_Script");
					serializedObject.ApplyModifiedProperties();
				}
			}

			public class BuildPreProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
			{
				public int callbackOrder => -200;

				public void OnPreprocessBuild(BuildReport report)
				{
					using (PreloadedAssets.Lease(out var preloaded))
					{
						foreach (var type in IterateAll())
						{
							if (EditorOnlyAttribute.IsDefined(type))
								continue;

							var asset = Retrieve(type);
							asset.PreProcessBuild();

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
							if (EditorOnlyAttribute.IsDefined(type))
								continue;

							var asset = Retrieve(type);

							preloaded.Remove(asset);
						}
					}
				}
			}

			#region Utility
			public static IEnumerable<Type> IterateAll()
			{
				return TypeCache.GetTypesDerivedFrom<ScriptableManager>()
					.Where(ScriptableManager.ManagerAttribute.IsDefined)
					.OrderBy(ScriptableManager.LoadOrderAttribute.GetOrder);
			}
			#endregion

			static Runtime()
			{
				dictionary = new Dictionary<Type, ScriptableManager>();
			}
#endif

			public static class Defaults
			{
				public static class LoadOrder
				{
					public const int AutoPreferences = -1000;
					public const int ProjectPreferences = -1000;
					public const int ScenesCollection = -1000;

					public const int LocalizationSystem = -998;

					public const int NarrativeSystem = 0;

					public const int ScriptableObjectInitializer = 10;
				}
			}
		}

		#region Attributes
		/// <summary>
		/// Attribute that needs to be applied to all Scriptable Manager instances for them to register in the system
		/// </summary>
		[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
		public sealed class ManagerAttribute : Attribute
		{
			public ManagerAttribute()
			{

			}

			public static ManagerAttribute Retrieve(Type type)
			{
				return type.GetCustomAttribute<ManagerAttribute>();
			}

#if UNITY_EDITOR
			public static bool IsDefined(Type type)
			{
				return type.GetCustomAttribute<ManagerAttribute>() != null;
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
		/// Attribute that specifies the load order of managers,
		/// by default every manager has an order of 0
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
	}

	public class ScriptableManager<T> : ScriptableManager
		where T : ScriptableManager<T>
	{
		public static T Instance { get; private set; }

		internal override void Prepare()
		{
			Instance = this as T;
			OnLoad();
		}
	}
}