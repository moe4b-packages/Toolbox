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

namespace MB
{
	public static class QueryComponent
	{
		#region Generic
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
			where T: class
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
					return InScene<T>(surrogate);

				case ComponentQueryScope.Global:
					return InGlobal<T>();

				default:
					throw new NotImplementedException();
            }
        }

		public static T InSelf<T>(UObjectSurrogate surrogate)
			where T : class
		{
			return surrogate.GameObject.GetComponent<T>();
		}

		public static T InChildren<T>(UObjectSurrogate surrogate, bool includeInactive = false)
			where T : class
		{
			return surrogate.GameObject.GetComponentInChildren<T>(includeInactive);
		}

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

		public static T InScene<T>(UObjectSurrogate surrogate, bool includeInactive = false) where T : class
		{
			return InScene<T>(surrogate.GameObject.scene, includeInactive: includeInactive);
		}
		public static T InScene<T>(Scene scene, bool includeInactive = false) where T : class
		{
			var roots = scene.GetRootGameObjects();

			for (int i = 0; i < roots.Length; i++)
			{
				var component = InChildren<T>(roots[i], includeInactive: includeInactive);

				if (component != null) return component;
			}

			return null;
		}

		public static T InGlobal<T>(bool incaludeInactive = false)
			where T : class
		{
			var target = typeof(T);

			if (target.IsInterface)
			{
				var behaviours = Object.FindObjectsOfType<MonoBehaviour>(incaludeInactive);

				for (int i = 0; i < behaviours.Length; i++)
					if (behaviours[i] is T component)
						return component;

				return null;
			}
			else
			{
				var component = Object.FindObjectOfType(target, incaludeInactive) as T;

				return component;
			}
		}
		#endregion

		#region Type
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
					return InScene(surrogate, type);

				case ComponentQueryScope.Global:
					return InGlobal(type);

				default:
					throw new NotImplementedException($"{scope}");
			}
		}

		public static Component InSelf(UObjectSurrogate surrogate, Type type)
		{
			return surrogate.GameObject.GetComponent(type);
		}

		public static Component InChildren(UObjectSurrogate surrogate, Type type, bool includeInactive = false)
		{
			return surrogate.GameObject.GetComponentInChildren(type, includeInactive);
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

		public static Component InScene(UObjectSurrogate surrogate, Type type, bool includeInactive = false)
		{
			return InScene(surrogate.GameObject.scene, type, includeInactive: includeInactive);
		}
		public static Component InScene(Scene scene, Type type, bool includeInactive = false)
		{
			var roots = scene.GetRootGameObjects();

			for (int i = 0; i < roots.Length; i++)
			{
				var component = InChildren(roots[i], type, includeInactive: includeInactive);

				if (component != null) return component;
			}

			return null;
		}

		public static Component InGlobal(Type type, bool includeInactive = false)
		{
			if (type.IsInterface)
			{
				var behaviours = Object.FindObjectsOfType<MonoBehaviour>(includeInactive);

				for (int i = 0; i < behaviours.Length; i++)
				{
					var context = behaviours[i].GetType();

					if (type.IsAssignableFrom(context))
						return behaviours[i];
				}

				return null;
			}
			else
			{
				var component = Object.FindObjectOfType(type, includeInactive) as Component;

				return component;
			}
		}
		#endregion

		public static ComponentQueryScope Self => ComponentQueryScope.Self;
		public static ComponentQueryScope Children => ComponentQueryScope.Children;
		public static ComponentQueryScope Parents => ComponentQueryScope.Parents;
		public static ComponentQueryScope Scene => ComponentQueryScope.Scene;
		public static ComponentQueryScope Global => ComponentQueryScope.Global;
	}

	public static class QueryComponents
	{
		#region Generic
		public static HashSet<T> In<T>(UObjectSurrogate surrogate, params ComponentQueryScope[] scopes)
			where T : class
		{
			var scope = ComponentQueryScope.None;

			for (int i = 0; i < scopes.Length; i++)
				scope |= scopes[i];

			return In<T>(surrogate, scope);
		}
		public static HashSet<T> In<T>(UObjectSurrogate surrogate, ComponentQueryScope scope)
			where T : class
		{
			var hashset = new HashSet<T>();

			if (scope.HasFlag(ComponentQueryScope.Global))
			{
				var range = InGlobal<T>();
				hashset.UnionWith(range);
			}
			else if(scope.HasFlag(ComponentQueryScope.Scene))
            {
				var range = InScene<T>(surrogate);
				hashset.UnionWith(range);
            }
			else
			{
				if (scope.HasFlag(ComponentQueryScope.Children))
				{
					var range = InChildren<T>(surrogate);
					hashset.UnionWith(range);
				}
				else if (scope.HasFlag(ComponentQueryScope.Self))
				{
					var range = InSelf<T>(surrogate);
					hashset.UnionWith(range);
				}

				if (scope.HasFlag(ComponentQueryScope.Parents))
				{
					var range = InParents<T>(surrogate);
					hashset.UnionWith(range);
				}
			}

			return hashset;
		}

		public static T[] InSelf<T>(UObjectSurrogate surrogate)
			where T : class
		{
			return surrogate.GameObject.GetComponents<T>();
		}

		public static T[] InChildren<T>(UObjectSurrogate surrogate, bool includeInactive = false)
			where T : class
		{
			return surrogate.GameObject.GetComponentsInChildren<T>(includeInactive);
		}

		public static T[] InParents<T>(UObjectSurrogate surrogate)
			where T : class
		{
			var list = new List<T>();

			var context = surrogate.Transform.parent;

			while (true)
			{
				if (context == null) break;

				context.GetComponents<T>(list);

				context = context.parent;
			}

			return list.ToArray();
		}

		public static List<T> InScene<T>(UObjectSurrogate surrogate, bool includeInactive = false) where T : class
		{
			return InScene<T>(surrogate.GameObject.scene, includeInactive: includeInactive);
		}
		public static List<T> InScene<T>(Scene scene, bool includeInactive = false) where T : class
		{
			var list = new List<T>();

			var roots = scene.GetRootGameObjects();

			for (int i = 0; i < roots.Length; i++)
			{
				var range = InChildren<T>(roots[i], includeInactive: includeInactive);
				list.AddRange(range);
			}

			return list;
		}

		public static T[] InGlobal<T>(bool incaludeInactive = false)
			where T : class
		{
			var target = typeof(T);

			var list = new List<T>();

			if (target.IsInterface)
			{
				var behaviours = Object.FindObjectsOfType<MonoBehaviour>(incaludeInactive);

				for (int i = 0; i < behaviours.Length; i++)
					if (behaviours[i] is T component)
						list.Add(component);
			}
			else
			{
				var components = Object.FindObjectsOfType(target, incaludeInactive);

				for (int i = 0; i < components.Length; i++)
					list.Add(components[i] as T);
			}

			return list.ToArray();
		}
		#endregion

		#region Type
		public static HashSet<Component> In(UObjectSurrogate surrogate, Type type, params ComponentQueryScope[] scopes)
		{
			var scope = ComponentQueryScope.None;

            for (int i = 0; i < scopes.Length; i++)
				scope |= scopes[i];

			return In(surrogate, type, scope);
		}
		public static HashSet<Component> In(UObjectSurrogate surrogate, Type type, ComponentQueryScope scope)
		{
			var hashset = new HashSet<Component>();

			if(scope.HasFlag(ComponentQueryScope.Global))
            {
				var range = InGlobal(type);
				hashset.UnionWith(range);
			}
			else if (scope.HasFlag(ComponentQueryScope.Scene))
			{
				var range = InScene(surrogate, type);
				hashset.UnionWith(range);
			}
			else
            {
				if (scope.HasFlag(ComponentQueryScope.Children))
				{
					var range = InChildren(surrogate, type);
					hashset.UnionWith(range);
				}
				else if (scope.HasFlag(ComponentQueryScope.Self))
				{
					var range = InSelf(surrogate, type);
					hashset.UnionWith(range);
				}

				if (scope.HasFlag(ComponentQueryScope.Parents))
				{
					var range = InParents(surrogate, type);
					hashset.UnionWith(range);
				}
			}

			return hashset;
		}

		public static Component[] InSelf(UObjectSurrogate surrogate, Type type)
		{
			return surrogate.GameObject.GetComponents(type);
		}

		public static Component[] InChildren(UObjectSurrogate surrogate, Type type, bool includeInactive = false)
		{
			return surrogate.GameObject.GetComponentsInChildren(type, includeInactive);
		}

		public static Component[] InParents(UObjectSurrogate surrogate, Type type)
		{
			var list = new List<Component>();

			var context = surrogate.Transform.parent;

			while (true)
			{
				if (context == null) break;

				context.GetComponents(type, list);

				context = context.parent;
			}

			return list.ToArray();
		}

		public static List<Component> InScene(UObjectSurrogate surrogate, Type type, bool includeInactive = false)
		{
			return InScene(surrogate.GameObject.scene, type, includeInactive: includeInactive);
		}
		public static List<Component> InScene(Scene scene, Type type, bool includeInactive = false)
		{
			var list = new List<Component>();

			var roots = scene.GetRootGameObjects();

			for (int i = 0; i < roots.Length; i++)
			{
				var range = InChildren(roots[i], type, includeInactive: includeInactive);
				list.AddRange(range);
			}

			return list;
		}

		public static Component[] InGlobal(Type type, bool incaludeInactive = false)
		{
			var list = new List<Component>();

			if (type.IsInterface)
			{
				var behaviours = Object.FindObjectsOfType<MonoBehaviour>(incaludeInactive);

				for (int i = 0; i < behaviours.Length; i++)
				{
					var context = behaviours[i].GetType();

					if (type.IsAssignableFrom(context))
						list.Add(behaviours[i]);
				}
			}
			else
			{
				var components = Object.FindObjectsOfType(type, incaludeInactive);

				for (int i = 0; i < components.Length; i++)
					list.Add(components[i] as Component);
			}

			return list.ToArray();
		}
		#endregion

		public static ComponentQueryScope Self => ComponentQueryScope.Self;
		public static ComponentQueryScope Children => ComponentQueryScope.Children;
		public static ComponentQueryScope Parents => ComponentQueryScope.Parents;
		public static ComponentQueryScope Scene => ComponentQueryScope.Scene;
		public static ComponentQueryScope Global => ComponentQueryScope.Global;
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