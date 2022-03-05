using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MB
{
	public interface IModules
	{
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
		List<Component> components;
		public List<Component> Components => components;

        #region Modifications
        public void Register(UObjectSurrogate target)
		{
			using(ComponentQuery.Collection.NonAlloc.InHierarchy<IModule<TReference>>(target, out var list))
            {
				for (int i = 0; i < list.Count; i++)
					components.Add(list[i] as Component);
			}
		}

		public void Register(IModules collection) => Register(collection, ModuleScope.Local);
		public void Register(IModules collection, ModuleScope scope)
		{
			var selection = collection.FindAll<IModule<TReference>>();

			for (int i = 0; i < selection.Count; i++)
				if (ValidateScope(Reference, selection[i], scope))
					components.Add(selection[i] as Component);
		}

		public void Clear()
		{
			components.Clear();
		}
        #endregion

		public void Set(TReference reference)
		{
			Reference = reference;

			for (int i = 0; i < components.Count; i++)
            {
				var target = components[i] as IModule<TReference>;
				target.Set(reference);
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

		public static bool ValidateScope(TReference reference, IModule<TReference> module, ModuleScope scope)
		{
			switch (scope)
			{
				case ModuleScope.Local:
					return module.Self.transform.IsChildOf(reference.transform);

				case ModuleScope.Global:
					return true;
			}

			throw new NotImplementedException();
		}
	}

    public interface IModule<TReference>
    {
		public GameObject Self { get; }

		void Set(TReference reference);
    }

	public enum ModuleScope
	{
		Local, Global
	}
}