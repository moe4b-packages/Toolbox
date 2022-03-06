using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MB
{
	public interface ISegment
	{
		public List<Component> Components { get; }

		TTarget Find<TTarget>()
				where TTarget : class;

		List<TTarget> FindAll<TTarget>()
			where TTarget : class;
	}

	[Serializable]
	public class Segments<TReference, TInterface> : ISegment
		where TReference : Component
		where TInterface : class
    {
		public TReference Reference { get; protected set; }

		[SerializeField]
		protected List<Component> components;
		public List<Component> Components => components;

		public void Prepare(TReference reference)
		{
			this.Reference = reference;

			Clear();
		}

		#region Register
		public void Register(GameObject gameObject)
		{
			using (ComponentQuery.Collection.NonAlloc.InHierarchy<TInterface>(gameObject, out var list))
			{
				for (int i = 0; i < list.Count; i++)
					components.Add(list[i] as Component);
			}
		}

		public void Register(ISegment collection) => Register(collection, SegmentScope.Local);
		public void Register(ISegment collection, SegmentScope scope)
		{
			for (int i = 0; i < collection.Components.Count; i++)
				if (collection.Components[i] is TInterface target)
					if (ValidateScope(Reference, collection.Components[i], scope))
						components.Add(collection.Components[i]);
		}
		#endregion

		public void Clear()
		{
			components.Clear();
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

		public Segments()
		{
			components = new List<Component>();
		}
        public Segments(TReference reference) : this()
        {
			Prepare(reference);
        }

		//Static Utility

		public static bool ValidateScope(TReference reference, Component module, SegmentScope scope)
		{
			switch (scope)
			{
				case SegmentScope.Local:
					return module.transform.IsChildOf(reference.transform);

				case SegmentScope.Global:
					return true;
			}

			throw new NotImplementedException();
		}
	}

	public enum SegmentScope
	{
		Local, Global
	}
}