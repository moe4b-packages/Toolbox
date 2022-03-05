using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MB
{
    public interface IModules
	{
		public List<Component> Components { get; }

		TTarget Find<TTarget>()
				where TTarget : class;

		List<TTarget> FindAll<TTarget>()
			where TTarget : class;
	}

	[Serializable]
	public class Modules<TReference> : IModules
		where TReference : Component
	{
		public TReference Reference { get; protected set; }

		[SerializeField]
		protected List<Component> components;
		public List<Component> Components => components;

		public void Configure(TReference reference)
		{
			this.Reference = reference;

			Clear();
		}

		#region Register
		public void Register(GameObject gameObject)
		{
			using (ComponentQuery.Collection.NonAlloc.InHierarchy<IModule<TReference>>(gameObject, out var list))
			{
				for (int i = 0; i < list.Count; i++)
					components.Add(list[i] as Component);
			}
		}

		public void Register(IModules collection) => Register(collection, ModuleScope.Local);
		public void Register(IModules collection, ModuleScope scope)
		{
			for (int i = 0; i < collection.Components.Count; i++)
				if (collection.Components[i] is IModule<TReference> module)
					if (ValidateScope(Reference, collection.Components[i], scope))
						components.Add(collection.Components[i]);
		}
		#endregion

		public void Clear()
		{
			components.Clear();
		}

		public void Set()
		{
			for (int i = 0; i < components.Count; i++)
			{
				var target = components[i] as IModule<TReference>;
				target.Set(Reference);
			}
		}

		#region Query
		public TTarget Find<TTarget>()
			where TTarget : class
		{
			ValidateQuery<TTarget>();

			for (int i = 0; i < components.Count; i++)
				if (components[i] is TTarget target)
					return target;

			return null;
		}

		public List<TTarget> FindAll<TTarget>()
			where TTarget : class
		{
			ValidateQuery<TTarget>();

			var selection = new List<TTarget>();

			for (int i = 0; i < components.Count; i++)
				if (components[i] is TTarget target)
					selection.Add(target);

			return selection;
		}

		public void ValidateQuery<TTarget>()
		{
#if UNITY_EDITOR
			var type = typeof(TTarget);
			if (type.IsInterface) return;

			var module = typeof(TTarget);

			if (module.IsAssignableFrom(type) == false)
				throw new Exception($"Invalid Query For {type.Name} Within Collection of {module.Name}'s" +
					$", Please Ensure that {type} Inherits from {module.Name}");
#endif
		}

		public TTarget Depend<TTarget>()
			where TTarget : class
		{
			var target = Find<TTarget>();

			return target;
		}
		#endregion

		public Modules()
		{
			components = new List<Component>();
		}

		//Static Utility

		public static bool ValidateScope(TReference reference, Component module, ModuleScope scope)
		{
			switch (scope)
			{
				case ModuleScope.Local:
					return module.transform.IsChildOf(reference.transform);

				case ModuleScope.Global:
					return true;
			}

			throw new NotImplementedException();
		}
	}

    public interface IModule<TReference>
		where TReference : Component
    {
		void Set(TReference reference);
	}

	public enum ModuleScope
	{
		Local, Global
	}
}