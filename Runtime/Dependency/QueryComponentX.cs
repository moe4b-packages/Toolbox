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
	/// supports querying for interfaces as well as classes
	/// </summary>
	public static class QueryComponentX
	{
		public static class Scope
		{
			public static Type Type { get; } = typeof(QueryComponentScope);

			public static readonly QueryComponentScope[] Defaults = new QueryComponentScope[]
			{
				QueryComponentScope.Self,
				QueryComponentScope.Children,
			};

			public static QueryComponentScope[] Values { get; private set; }

			public static IList<QueryComponentScope> FlagToArray(QueryComponentScope flag)
			{
				var list = new List<QueryComponentScope>();

				for (int i = 0; i < Values.Length; i++)
					if (flag.HasFlag(Values[i]))
						list.Add(Values[i]);

				return list;
			}
			public static QueryComponentScope ArrayToFlag(params QueryComponentScope[] array)
			{
				var value = QueryComponentScope.None;

				for (int i = 0; i < array.Length; i++)
					value |= array[i];

				return value;
			}

			static Scope()
			{
				Values = (QueryComponentScope[])Enum.GetValues(Type);
			}
		}
	}

	public static class QueryComponent
	{
		public static QueryComponentScope Self => QueryComponentScope.Self;
		public static QueryComponentScope Children => QueryComponentScope.Children;
		public static QueryComponentScope Parents => QueryComponentScope.Parents;
		public static QueryComponentScope Scene => QueryComponentScope.Scene;
		public static QueryComponentScope Global => QueryComponentScope.Global;

		#region Main
		public static T In<T>(UObjectSurrogate surrogate)
			where T : class
		{
			return In<T>(surrogate, QueryComponentX.Scope.Defaults);
		}
		public static T In<T>(UObjectSurrogate surrogate, params QueryComponentScope[] scopes)
			where T : class
		{
			for (int i = 0; i < scopes.Length; i++)
			{
				var component = In<T>(surrogate, scopes[i]);

				if (component != null) return component;
			}

			return null;
		}
		public static T In<T>(UObjectSurrogate surrogate, QueryComponentScope scope)
			where T : class
		{
			switch (scope)
			{
				case QueryComponentScope.Self:
					return InSelf<T>(surrogate);

				case QueryComponentScope.Children:
					return InChildren<T>(surrogate);

				case QueryComponentScope.Parents:
					return InParents<T>(surrogate);

				case QueryComponentScope.Scene:
					return InScene<T>(surrogate.Scene);

				case QueryComponentScope.Global:
					return InGlobal<T>();

				default:
					throw new NotImplementedException();
			}
		}

		public static Component In(UObjectSurrogate surrogate, Type type)
		{
			return In(surrogate, type, QueryComponentX.Scope.Defaults);
		}
		public static Component In(UObjectSurrogate surrogate, Type type, params QueryComponentScope[] scopes)
		{
			for (int i = 0; i < scopes.Length; i++)
			{
				var component = In(surrogate, type, scopes[i]);

				if (component != null) return component;
			}

			return null;
		}
		public static Component In(UObjectSurrogate surrogate, Type type, QueryComponentScope scope)
		{
			switch (scope)
			{
				case QueryComponentScope.Self:
					return InSelf(surrogate, type);

				case QueryComponentScope.Children:
					return InChildren(surrogate, type);

				case QueryComponentScope.Parents:
					return InParents(surrogate, type);

				case QueryComponentScope.Scene:
					return InScene(surrogate.Scene, type);

				case QueryComponentScope.Global:
					return InGlobal(type);

				default:
					throw new NotImplementedException($"{scope}");
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

	public static class QueryComponents
	{
		public static QueryComponentScope Self => QueryComponentScope.Self;
		public static QueryComponentScope Children => QueryComponentScope.Children;
		public static QueryComponentScope Parents => QueryComponentScope.Parents;
		public static QueryComponentScope Scene => QueryComponentScope.Scene;
		public static QueryComponentScope Global => QueryComponentScope.Global;

		public static class Cache
		{
			public static class InternalMethod
			{
				public const string Name = "GetComponentsInternal";

				public const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

				public static readonly Delegate Binding;
				public delegate Array Delegate(GameObject gameObject, Type type, bool generic, bool recursive, bool includeInactive, bool reverse, object list);

				static InternalMethod()
				{
					var type = typeof(GameObject);

					var method = type.GetMethod(Name, Flags);

					Binding = method.CreateDelegate<Delegate>();
				}
			}
		}

		#region Main
		//Generic
		public static void In<T>(UObjectSurrogate surrogate, List<T> list)
			where T : class
		{
			var flag = QueryComponentScope.Self | QueryComponentScope.Children;

			In<T>(surrogate, list, flag);
		}
		public static void In<T>(UObjectSurrogate surrogate, List<T> list, params QueryComponentScope[] scopes)
			where T : class
		{
			var flag = QueryComponentX.Scope.ArrayToFlag(scopes);

			In<T>(surrogate, list, flag);
		}
		public static void In<T>(UObjectSurrogate surrogate, List<T> list, QueryComponentScope scope)
			where T : class
		{
			if (scope.HasFlag(QueryComponentScope.Global))
			{
				InGlobal<T>(list);
			}
			else if (scope.HasFlag(QueryComponentScope.Scene))
			{
				InScene<T>(surrogate.Scene, list);
			}
			else
			{
				if (scope.HasFlag(QueryComponentScope.Self))
				{
					InSelf<T>(surrogate, list);
				}
				if (scope.HasFlag(QueryComponentScope.Children))
				{
					InChildren<T>(surrogate, list);
				}
				if (scope.HasFlag(QueryComponentScope.Parents))
				{
					InParents<T>(surrogate, list);
				}
			}
		}

		public static List<T> In<T>(UObjectSurrogate surrogate)
			where T : class
		{
			var flag = QueryComponentScope.Self | QueryComponentScope.Children;

			return In<T>(surrogate, flag);
		}
		public static List<T> In<T>(UObjectSurrogate surrogate, params QueryComponentScope[] scopes)
			where T : class
		{
			var flag = QueryComponentX.Scope.ArrayToFlag(scopes);

			return In<T>(surrogate, flag);
		}
		public static List<T> In<T>(UObjectSurrogate surrogate, QueryComponentScope scope)
			where T : class
		{
			var list = new List<T>();

			In<T>(surrogate, list, scope);

			return list;
		}

		//Typed
		public static void In(UObjectSurrogate surrogate, Type type, List<Component> list)
		{
			var flag = QueryComponentScope.Self | QueryComponentScope.Children;

			In(surrogate, type, list, flag);
		}
		public static void In(UObjectSurrogate surrogate, Type type, List<Component> list, params QueryComponentScope[] scopes)
		{
			var flag = QueryComponentX.Scope.ArrayToFlag(scopes);

			In(surrogate, type, list, flag);
		}
		public static void In(UObjectSurrogate surrogate, Type type, List<Component> list, QueryComponentScope scope)
		{
			if (scope.HasFlag(QueryComponentScope.Global))
			{
				InGlobal(type, list);
			}
			else if (scope.HasFlag(QueryComponentScope.Scene))
			{
				InScene(surrogate.Scene, type, list);
			}
			else
			{
				if (scope.HasFlag(QueryComponentScope.Self))
				{
					InSelf(surrogate, type, list);
				}
				if (scope.HasFlag(QueryComponentScope.Children))
				{
					InChildren(surrogate, type, list);
				}
				if (scope.HasFlag(QueryComponentScope.Parents))
				{
					InParents(surrogate, type, list);
				}
			}
		}

		public static List<Component> In(UObjectSurrogate surrogate, Type type)
		{
			var flag = QueryComponentScope.Self | QueryComponentScope.Children;

			return In(surrogate, type, flag);
		}
		public static List<Component> In(UObjectSurrogate surrogate, Type type, params QueryComponentScope[] scopes)
		{
			var flag = QueryComponentX.Scope.ArrayToFlag(scopes);

			return In(surrogate, type, flag);
		}
		public static List<Component> In(UObjectSurrogate surrogate, Type type, QueryComponentScope scope)
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
			using (DisposablePool.List<T>.Lease(out var temp))
			{
				surrogate.GameObject.GetComponents(temp);
				list.AddRange(temp);
			}
		}
		public static T[] InSelf<T>(UObjectSurrogate surrogate)
			where T : class
		{
			return surrogate.GameObject.GetComponents<T>();
		}

		public static void InSelf(UObjectSurrogate surrogate, Type type, List<Component> list)
		{
			using (DisposablePool.List<Component>.Lease(out var temp))
			{
				surrogate.GameObject.GetComponents(type, temp);
				list.AddRange(temp);
			}
		}
		public static Component[] InSelf(UObjectSurrogate surrogate, Type type)
		{
			return surrogate.GameObject.GetComponents(type);
		}
		#endregion

		#region Children
		public static void InChildren<T>(UObjectSurrogate surrogate, List<T> list)
			where T : class
		{
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
			var transform = surrogate.Transform;

			using (DisposablePool.List<Component>.Lease(out var temp))
			{
				for (int i = 0; i < transform.childCount; i++)
				{
					var gameObject = transform.GetChild(i).gameObject;

					Cache.InternalMethod.Binding(gameObject, type, false, true, true, false, temp);

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

		#region Parents
		public static void InParents<T>(UObjectSurrogate surrogate, List<T> list)
		{
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
			using (DisposablePool.List<T>.Lease(out var temp))
			{
				var roots = scene.GetRootGameObjects();

				for (int i = 0; i < roots.Length; i++)
				{
					InSelf<T>(roots[i], temp);
					InChildren<T>(roots[i], temp);
					list.AddRange(temp);
					temp.Clear();
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
			using (DisposablePool.List<Component>.Lease(out var temp))
			{
				var roots = scene.GetRootGameObjects();

				for (int i = 0; i < roots.Length; i++)
				{
					InSelf(roots[i], type, temp);
					InChildren(roots[i], type, temp);
					list.AddRange(temp);
					temp.Clear();
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
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
				var scene = SceneManager.GetSceneAt(i);

				InScene<T>(scene, list);
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
			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				var scene = SceneManager.GetSceneAt(i);

				InScene(scene, type, list);
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
	}

	[Flags]
	public enum QueryComponentScope
	{
		None = 0,

		Self = 1 << 0,
		Children = 1 << 1,
		Parents = 1 << 2,
		Scene = 1 << 3,
		Global = 1 << 4,
	}
}