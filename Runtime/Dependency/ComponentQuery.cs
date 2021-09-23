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
using System.Reflection;

namespace MB
{
	/// <summary>
	/// A helper class to query for components using types or generics,
	/// supports querying for interfaces as well as classes,
	/// Some FAQ:
	/// 1. InChildren will actually only search the children of the provided object,
	/// if you want to query the object it's self and it's children then use InHierarchy.
	/// 
	/// 2. Every InXXX method overload that take a List(X) will clear any previous entries in that list,
	/// this is default behaviour that is present in the Unity query methods, and as I'm using them internally
	/// I had to follow that behaviour (even tho I don't like it at all).
	/// </summary>
	public static class ComponentQuery
	{
		public static class Scope
		{
			public static Type Type { get; } = typeof(ComponentQueryScope);

			public static class Default
			{
				public const ComponentQueryScope Flag = ComponentQueryScope.Self | ComponentQueryScope.Children;

				public static readonly ComponentQueryScope[] Array = new ComponentQueryScope[]
				{
					ComponentQueryScope.Self,
					ComponentQueryScope.Children,
				};
			}

			public static ComponentQueryScope[] Values { get; private set; }

			public static IList<ComponentQueryScope> FlagToArray(ComponentQueryScope flag)
			{
				var list = new List<ComponentQueryScope>();

				for (int i = 0; i < Values.Length; i++)
					if (flag.HasFlag(Values[i]))
						list.Add(Values[i]);

				return list;
			}
			public static ComponentQueryScope ArrayToFlag(params ComponentQueryScope[] array)
			{
				var value = ComponentQueryScope.None;

				for (int i = 0; i < array.Length; i++)
					value |= array[i];

				return value;
			}

			static Scope()
			{
				Values = (ComponentQueryScope[])Enum.GetValues(Type);
			}
		}

		static class InternalMethod
		{
			public const string Name = "GetComponentsInternal";

			public const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

			static readonly Delegate Binding;
			public delegate Array Delegate(GameObject gameObject, Type type, bool generic, bool recursive, bool includeInactive, bool reverse, object list);

			public static void Invoke(GameObject gameObject, Type type, List<Component> list)
			{
				Binding(gameObject, type, false, true, true, false, list);
			}

			static InternalMethod()
			{
				var type = typeof(GameObject);

				var method = type.GetMethod(Name, Flags);

				Binding = method.CreateDelegate<Delegate>();
			}
		}

		public static ComponentQueryScope Self => ComponentQueryScope.Self;
		public static ComponentQueryScope Children => ComponentQueryScope.Children;
		public static ComponentQueryScope Parents => ComponentQueryScope.Parents;
		public static ComponentQueryScope Scene => ComponentQueryScope.Scene;
		public static ComponentQueryScope Global => ComponentQueryScope.Global;

		public static class Single
		{
			#region Main
			public static T In<T>(UObjectSurrogate surrogate)
				where T : class
			{
				return In<T>(surrogate, Scope.Default.Array);
			}
			public static T In<T>(UObjectSurrogate surrogate, params ComponentQueryScope[] scopes)
				where T : class
			{
				for (int i = 0; i < scopes.Length; i++)
				{
					var component = In<T>(surrogate, scopes[i]);

					if (component != null) return component;
				}

				return null;
			}
			public static T In<T>(UObjectSurrogate surrogate, ComponentQueryScope scope)
				where T : class
			{
				switch (scope)
				{
					case ComponentQueryScope.Self:
						return InSelf<T>(surrogate);

					case ComponentQueryScope.Children:
						return InChildren<T>(surrogate);

					case ComponentQueryScope.Parents:
						return InParents<T>(surrogate);

					case ComponentQueryScope.Scene:
						return InScene<T>(surrogate.Scene);

					case ComponentQueryScope.Global:
						return InGlobal<T>();

					default:
						throw new ArgumentException($"Invalid Argument of {scope}, This method doesn't accept flag values or None, please use the params instead of using flags");
				}
			}

			public static Component In(UObjectSurrogate surrogate, Type type)
			{
				return In(surrogate, type, Scope.Default.Array);
			}
			public static Component In(UObjectSurrogate surrogate, Type type, params ComponentQueryScope[] scopes)
			{
				for (int i = 0; i < scopes.Length; i++)
				{
					var component = In(surrogate, type, scopes[i]);

					if (component != null) return component;
				}

				return null;
			}
			public static Component In(UObjectSurrogate surrogate, Type type, ComponentQueryScope scope)
			{
				switch (scope)
				{
					case ComponentQueryScope.Self:
						return InSelf(surrogate, type);

					case ComponentQueryScope.Children:
						return InChildren(surrogate, type);

					case ComponentQueryScope.Parents:
						return InParents(surrogate, type);

					case ComponentQueryScope.Scene:
						return InScene(surrogate.Scene, type);

					case ComponentQueryScope.Global:
						return InGlobal(type);

					default:
						throw new ArgumentException($"Invalid Argument of {scope}, This method doesn't accept flag values or None, please use the params instead of using flags");
				}
			}
			#endregion

			#region Self
			public static T InSelf<T>(UObjectSurrogate surrogate)
				where T : class
			{
				return surrogate.GameObject.GetComponent<T>();
			}

			public static Component InSelf(UObjectSurrogate surrogate, Type type)
			{
				return surrogate.GameObject.GetComponent(type);
			}
			#endregion

			#region Children
			public static T InChildren<T>(UObjectSurrogate surrogate)
				where T : class
			{
				var transform = surrogate.Transform;

				for (int i = 0; i < transform.childCount; i++)
				{
					var component = transform.GetChild(i).GetComponentInChildren<T>(true);

					if (component != null) return component;
				}

				return null;
			}

			public static Component InChildren(UObjectSurrogate surrogate, Type type)
			{
				var transform = surrogate.Transform;

				for (int i = 0; i < transform.childCount; i++)
				{
					var component = transform.GetChild(i).GetComponentInChildren(type, true);

					if (component != null) return component;
				}

				return null;
			}
			#endregion

			#region Hierarchy
			public static T InHierarchy<T>(UObjectSurrogate surrogate)
				where T : class
			{
				return surrogate.GameObject.GetComponentInChildren<T>(true);
			}

			public static Component InHierarchy(UObjectSurrogate surrogate, Type type)
			{
				return surrogate.GameObject.GetComponentInChildren(type, true);
			}
			#endregion

			#region Parents
			public static T InParents<T>(UObjectSurrogate surrogate)
				where T : class
			{
				var context = surrogate.Transform.parent;

				while (true)
				{
					if (context == null) return null;

					if (context.TryGetComponent<T>(out var component))
						return component;

					context = context.parent;
				}
			}

			public static Component InParents(UObjectSurrogate surrogate, Type type)
			{
				var context = surrogate.Transform.parent;

				while (true)
				{
					if (context == null) return null;

					if (context.TryGetComponent(type, out var component))
						return component;

					context = context.parent;
				}
			}
			#endregion

			#region Scene
			public static T InScene<T>(Scene scene) where T : class
			{
				var roots = scene.GetRootGameObjects();

				for (int i = 0; i < roots.Length; i++)
				{
					var component = In<T>(roots[i]);

					if (component != null) return component;
				}

				return null;
			}

			public static Component InScene(Scene scene, Type type)
			{
				var roots = scene.GetRootGameObjects();

				for (int i = 0; i < roots.Length; i++)
				{
					var component = In(roots[i], type);

					if (component != null) return component;
				}

				return null;
			}
			#endregion

			#region Global
			public static T InGlobal<T>()
				where T : class
			{
				for (int i = 0; i < SceneManager.sceneCount; i++)
				{
					var scene = SceneManager.GetSceneAt(i);

					var component = InScene<T>(scene);

					if (component != null)
						return component;
				}

				return null;
			}

			public static Component InGlobal(Type type)
			{
				for (int i = 0; i < SceneManager.sceneCount; i++)
				{
					var scene = SceneManager.GetSceneAt(i);

					var component = InScene(scene, type);

					if (component != null)
						return component;
				}

				return null;
			}
			#endregion
		}

		public static class Collection
		{
			#region Main
			//Generic
			public static void In<T>(UObjectSurrogate surrogate, List<T> list)
				where T : class
			{
				In<T>(surrogate, list, Scope.Default.Flag);
			}
			public static void In<T>(UObjectSurrogate surrogate, List<T> list, params ComponentQueryScope[] scopes)
				where T : class
			{
				var flag = Scope.ArrayToFlag(scopes);

				In<T>(surrogate, list, flag);
			}
			public static void In<T>(UObjectSurrogate surrogate, List<T> list, ComponentQueryScope scope)
				where T : class
			{
				list.Clear();

				if (scope.HasFlag(ComponentQueryScope.Global))
				{
					InGlobal<T>(list);
				}
				else if (scope.HasFlag(ComponentQueryScope.Scene))
				{
					InScene<T>(surrogate.Scene, list);
				}
				else
				{
					var self = scope.HasFlag(ComponentQueryScope.Self);
					var children = scope.HasFlag(ComponentQueryScope.Children);

					if (self && children)
					{
						InHierarchy<T>(surrogate, list);
					}
					else if (self)
					{
						InSelf<T>(surrogate, list);
					}
					else if (children)
					{
						InChildren<T>(surrogate, list);
					}

					if (scope.HasFlag(ComponentQueryScope.Parents))
					{
						if (self || children)
						{
							using (DisposablePool.List<T>.Lease(out var temp))
							{
								InParents<T>(surrogate, temp);
								list.AddRange(temp);
							}
						}
						else
						{
							InParents<T>(surrogate, list);
						}
					}
				}
			}

			public static List<T> In<T>(UObjectSurrogate surrogate)
				where T : class
			{
				return In<T>(surrogate, Scope.Default.Flag);
			}
			public static List<T> In<T>(UObjectSurrogate surrogate, params ComponentQueryScope[] scopes)
				where T : class
			{
				var flag = Scope.ArrayToFlag(scopes);

				return In<T>(surrogate, flag);
			}
			public static List<T> In<T>(UObjectSurrogate surrogate, ComponentQueryScope scope)
				where T : class
			{
				var list = new List<T>();

				In<T>(surrogate, list, scope);

				return list;
			}

			//Typed
			public static void In(UObjectSurrogate surrogate, Type type, List<Component> list)
			{
				var flag = ComponentQueryScope.Self | ComponentQueryScope.Children;

				In(surrogate, type, list, flag);
			}
			public static void In(UObjectSurrogate surrogate, Type type, List<Component> list, params ComponentQueryScope[] scopes)
			{
				var flag = Scope.ArrayToFlag(scopes);

				In(surrogate, type, list, flag);
			}
			public static void In(UObjectSurrogate surrogate, Type type, List<Component> list, ComponentQueryScope scope)
			{
				list.Clear();

				if (scope.HasFlag(ComponentQueryScope.Global))
				{
					InGlobal(type, list);
				}
				else if (scope.HasFlag(ComponentQueryScope.Scene))
				{
					InScene(surrogate.Scene, type, list);
				}
				else
				{
					var self = scope.HasFlag(ComponentQueryScope.Self);
					var children = scope.HasFlag(ComponentQueryScope.Children);

					if (self && children)
					{
						InHierarchy(surrogate, type, list);
					}
					else if (self)
					{
						InSelf(surrogate, type, list);
					}
					else if (children)
					{
						InChildren(surrogate, type, list);
					}

					if (scope.HasFlag(ComponentQueryScope.Parents))
					{
						if (self || children)
						{
							using (DisposablePool.List<Component>.Lease(out var temp))
							{
								InParents(surrogate, type, temp);
								list.AddRange(temp);
							}
						}
						else
						{
							InParents(surrogate, type, list);
						}
					}
				}
			}

			public static List<Component> In(UObjectSurrogate surrogate, Type type)
			{
				var flag = ComponentQueryScope.Self | ComponentQueryScope.Children;

				return In(surrogate, type, flag);
			}
			public static List<Component> In(UObjectSurrogate surrogate, Type type, params ComponentQueryScope[] scopes)
			{
				var flag = Scope.ArrayToFlag(scopes);

				return In(surrogate, type, flag);
			}
			public static List<Component> In(UObjectSurrogate surrogate, Type type, ComponentQueryScope scope)
			{
				var list = new List<Component>();

				In(surrogate, type, list, scope);

				return list;
			}
			#endregion

			#region Self
			public static void InSelf<T>(UObjectSurrogate surrogate, List<T> list)
				where T : class
			{
				surrogate.GameObject.GetComponents(list);
			}
			public static T[] InSelf<T>(UObjectSurrogate surrogate)
				where T : class
			{
				return surrogate.GameObject.GetComponents<T>();
			}

			public static void InSelf(UObjectSurrogate surrogate, Type type, List<Component> list)
			{
				surrogate.GameObject.GetComponents(type, list);
			}
			public static Component[] InSelf(UObjectSurrogate surrogate, Type type)
			{
				return surrogate.GameObject.GetComponents(type);
			}
			#endregion

			#region Children
			//Scans the children of the provided object, only the children! not the object it's self

			public static void InChildren<T>(UObjectSurrogate surrogate, List<T> list)
				where T : class
			{
				list.Clear();

				var transform = surrogate.Transform;

				using (DisposablePool.List<T>.Lease(out var temp))
				{
					for (int i = 0; i < transform.childCount; i++)
					{
						transform.GetChild(i).GetComponentsInChildren(true, temp);
						list.AddRange(temp);
					}
				}
			}
			public static T[] InChildren<T>(UObjectSurrogate surrogate)
				where T : class
			{
				using (DisposablePool.List<T>.Lease(out var list))
				{
					InChildren(surrogate, list);
					return list.ToArray();
				}
			}

			public static void InChildren(UObjectSurrogate surrogate, Type type, List<Component> list)
			{
				list.Clear();

				var transform = surrogate.Transform;

				using (DisposablePool.List<Component>.Lease(out var temp))
				{
					for (int i = 0; i < transform.childCount; i++)
					{
						var gameObject = transform.GetChild(i).gameObject;
						InternalMethod.Invoke(gameObject, type, temp);
						list.AddRange(temp);
					}
				}
			}
			public static Component[] InChildren(UObjectSurrogate surrogate, Type type)
			{
				using (DisposablePool.List<Component>.Lease(out var list))
				{
					InChildren(surrogate, type, list);
					return list.ToArray();
				}
			}
			#endregion

			#region Hierarchy
			//Scans the Hierarchy of the provided object (self & children)

			public static void InHierarchy<T>(UObjectSurrogate surrogate, List<T> list)
				where T : class
			{
				surrogate.GameObject.GetComponentsInChildren<T>(true, list);
			}
			public static T[] InHierarchy<T>(UObjectSurrogate surrogate)
				where T : class
			{
				using (DisposablePool.List<T>.Lease(out var list))
				{
					InHierarchy(surrogate, list);
					return list.ToArray();
				}
			}

			public static void InHierarchy(UObjectSurrogate surrogate, Type type, List<Component> list)
			{
				InternalMethod.Invoke(surrogate.GameObject, type, list);
			}
			public static Component[] InHierarchy(UObjectSurrogate surrogate, Type type)
			{
				using (DisposablePool.List<Component>.Lease(out var list))
				{
					InHierarchy(surrogate, type, list);
					return list.ToArray();
				}
			}
			#endregion

			#region Parents
			public static void InParents<T>(UObjectSurrogate surrogate, List<T> list)
			{
				list.Clear();

				using (DisposablePool.List<T>.Lease(out var temp))
				{
					var context = surrogate.Transform.parent;

					while (true)
					{
						if (context == null) break;

						context.GetComponents(list);
						list.AddRange(temp);

						context = context.parent;
					}
				}
			}
			public static T[] InParents<T>(UObjectSurrogate surrogate)
				where T : class
			{
				using (DisposablePool.List<T>.Lease(out var list))
				{
					InParents(surrogate, list);
					return list.ToArray();
				}
			}

			public static void InParents(UObjectSurrogate surrogate, Type type, List<Component> list)
			{
				list.Clear();

				using (DisposablePool.List<Component>.Lease(out var temp))
				{
					var context = surrogate.Transform.parent;

					while (true)
					{
						if (context == null) break;

						context.GetComponents(type, list);
						list.AddRange(temp);

						context = context.parent;
					}
				}
			}
			public static Component[] InParents(UObjectSurrogate surrogate, Type type)
			{
				using (DisposablePool.List<Component>.Lease(out var list))
				{
					InParents(surrogate, type, list);
					return list.ToArray();
				}
			}
			#endregion

			#region Scene
			public static void InScene<T>(Scene scene, List<T> list)
				where T : class
			{
				list.Clear();

				using (DisposablePool.List<T>.Lease(out var temp))
				{
					var roots = scene.GetRootGameObjects();

					for (int i = 0; i < roots.Length; i++)
					{
						InHierarchy<T>(roots[i], temp);
						list.AddRange(temp);
					}
				}
			}
			public static T[] InScene<T>(Scene scene)
				where T : class
			{
				using (DisposablePool.List<T>.Lease(out var list))
				{
					InScene<T>(scene, list);
					return list.ToArray();
				}
			}

			public static void InScene(Scene scene, Type type, List<Component> list)
			{
				list.Clear();

				using (DisposablePool.List<Component>.Lease(out var temp))
				{
					var roots = scene.GetRootGameObjects();

					for (int i = 0; i < roots.Length; i++)
					{
						temp.Clear();
						InHierarchy(roots[i], type, temp);
						list.AddRange(temp);
					}
				}
			}
			public static Component[] InScene(Scene scene, Type type)
			{
				using (DisposablePool.List<Component>.Lease(out var list))
				{
					InScene(scene, type, list);
					return list.ToArray();
				}
			}
			#endregion

			#region Global
			public static void InGlobal<T>(List<T> list)
				where T : class
			{
				list.Clear();

				using (DisposablePool.List<T>.Lease(out var temp))
				{
					for (int i = 0; i < SceneManager.sceneCount; i++)
					{
						var scene = SceneManager.GetSceneAt(i);
						InScene<T>(scene, temp);
						list.AddRange(temp);
					}
				}
			}
			public static T[] InGlobal<T>()
				where T : class
			{
				using (DisposablePool.List<T>.Lease(out var list))
				{
					InGlobal<T>(list);
					return list.ToArray();
				}
			}

			public static void InGlobal(Type type, List<Component> list)
			{
				list.Clear();

				using (DisposablePool.List<Component>.Lease(out var temp))
				{
					for (int i = 0; i < SceneManager.sceneCount; i++)
					{
						var scene = SceneManager.GetSceneAt(i);
						InScene(scene, type, temp);
						list.AddRange(temp);
					}
				}
			}
			public static Component[] InGlobal(Type type)
			{
				using (DisposablePool.List<Component>.Lease(out var list))
				{
					InGlobal(type, list);
					return list.ToArray();
				}
			}
			#endregion

			/// <summary>
			/// Non allocating component queries uses Disposable Pool under the hood
			/// </summary>
			public static class NonAlloc
			{
				#region Main
				public static DisposablePool.Handle<List<T>> In<T>(UObjectSurrogate surrogate, out List<T> list)
					where T : class
				{
					var flag = ComponentQueryScope.Self | ComponentQueryScope.Children;

					return In<T>(surrogate, out list, flag);
				}
				public static DisposablePool.Handle<List<T>> In<T>(UObjectSurrogate surrogate, out List<T> list, params ComponentQueryScope[] scopes)
					where T : class
				{
					var flag = Scope.ArrayToFlag(scopes);

					return In<T>(surrogate, out list, flag);
				}
				public static DisposablePool.Handle<List<T>> In<T>(UObjectSurrogate surrogate, out List<T> list, ComponentQueryScope scope)
					where T : class
				{
					var handle = DisposablePool.List<T>.Lease(out list);
					Collection.In(surrogate, list, scope);
					return handle;
				}

				public static DisposablePool.Handle<List<Component>> In(UObjectSurrogate surrogate, Type type, out List<Component> list)
				{
					var flag = ComponentQueryScope.Self | ComponentQueryScope.Children;

					return In(surrogate, type, out list, flag);
				}
				public static DisposablePool.Handle<List<Component>> In(UObjectSurrogate surrogate, Type type, out List<Component> list, params ComponentQueryScope[] scopes)
				{
					var flag = Scope.ArrayToFlag(scopes);

					return In(surrogate, type, out list, flag);
				}
				public static DisposablePool.Handle<List<Component>> In(UObjectSurrogate surrogate, Type type, out List<Component> list, ComponentQueryScope scope)
				{
					var handle = DisposablePool.List<Component>.Lease(out list);
					Collection.In(surrogate, type, list, scope);
					return handle;
				}
				#endregion

				#region Self
				public static DisposablePool.Handle<List<T>> InSelf<T>(UObjectSurrogate surrogate, out List<T> list)
					where T : class
				{
					var handle = DisposablePool.List<T>.Lease(out list);
					Collection.InSelf<T>(surrogate, list);
					return handle;
				}

				public static DisposablePool.Handle<List<Component>> InSelf(UObjectSurrogate surrogate, Type type, out List<Component> list)
				{
					var handle = DisposablePool.List<Component>.Lease(out list);
					Collection.InSelf(surrogate, type, list);
					return handle;
				}
				#endregion

				#region Children
				public static DisposablePool.Handle<List<T>> InChildren<T>(UObjectSurrogate surrogate, out List<T> list)
					where T : class
				{
					var handle = DisposablePool.List<T>.Lease(out list);
					Collection.InChildren<T>(surrogate, list);
					return handle;
				}

				public static DisposablePool.Handle<List<Component>> InChildren(UObjectSurrogate surrogate, Type type, out List<Component> list)
				{
					var handle = DisposablePool.List<Component>.Lease(out list);
					Collection.InChildren(surrogate, type, list);
					return handle;
				}
				#endregion

				#region Hierarchy
				public static DisposablePool.Handle<List<T>> InHierarchy<T>(UObjectSurrogate surrogate, out List<T> list)
					where T : class
				{
					var handle = DisposablePool.List<T>.Lease(out list);
					Collection.InHierarchy<T>(surrogate, list);
					return handle;
				}

				public static DisposablePool.Handle<List<Component>> InHierarchy(UObjectSurrogate surrogate, Type type, out List<Component> list)
				{
					var handle = DisposablePool.List<Component>.Lease(out list);
					Collection.InHierarchy(surrogate, type, list);
					return handle;
				}
				#endregion

				#region Parents
				public static DisposablePool.Handle<List<T>> InParents<T>(UObjectSurrogate surrogate, out List<T> list)
				{
					var handle = DisposablePool.List<T>.Lease(out list);
					Collection.InParents<T>(surrogate, list);
					return handle;
				}

				public static DisposablePool.Handle<List<Component>> InParents(UObjectSurrogate surrogate, Type type, out List<Component> list)
				{
					var handle = DisposablePool.List<Component>.Lease(out list);
					Collection.InParents(surrogate, type, list);
					return handle;
				}
				#endregion

				#region Scene
				public static DisposablePool.Handle<List<T>> InScene<T>(Scene scene, out List<T> list)
					where T : class
				{
					var handle = DisposablePool.List<T>.Lease(out list);
					Collection.InScene<T>(scene, list);
					return handle;
				}

				public static DisposablePool.Handle<List<Component>> InScene<T>(Scene scene, Type type, out List<Component> list)
				{
					var handle = DisposablePool.List<Component>.Lease(out list);
					Collection.InScene(scene, type, list);
					return handle;
				}
				#endregion

				#region Global
				public static DisposablePool.Handle<List<T>> InGlobal<T>(out List<T> list)
					where T : class
				{
					var handle = DisposablePool.List<T>.Lease(out list);
					Collection.InGlobal<T>(list);
					return handle;
				}

				public static DisposablePool.Handle<List<Component>> InGlobal(Type type, out List<Component> list)
				{
					var handle = DisposablePool.List<Component>.Lease(out list);
					Collection.InGlobal(type, list);
					return handle;
				}
				#endregion
			}
		}
	}

	[Flags]
	public enum ComponentQueryScope
	{
		None = 0,

		Self = 1 << 0,
		Children = 1 << 1,
		Parents = 1 << 2,
		Scene = 1 << 3,
		Global = 1 << 4,
	}
}