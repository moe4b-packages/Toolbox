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
	public interface IGenericModularCollection
	{
		TTarget Find<TTarget>()
				where TTarget : class;

		List<TTarget> FindAll<TTarget>()
			where TTarget : class;
	}

	public class GenericModularCollection<TItem>
	{
		public List<TItem> List { get; protected set; }

		public virtual void Add(TItem module)
		{
			List.Add(module);
		}

		public virtual void AddAll(IEnumerable<TItem> range)
		{
			foreach (var item in range)
				Add(item);
		}

		public virtual bool Remove(TItem module)
		{
			return List.Remove(module);
		}

		#region Iteration
		public void ForAll(Action<TItem> action)
		{
			for (int i = 0; i < List.Count; i++)
				action(List[i]);
		}

		public void ForAll<TTarget>(Action<TTarget> action)
			where TTarget : class
		{
			for (int i = 0; i < List.Count; i++)
				if (List[i] is TTarget target)
					action(target);
		}
		#endregion

		#region Query
		public TTarget Find<TTarget>()
			where TTarget : class
		{
			ValidateQuery<TTarget>();

			for (int i = 0; i < List.Count; i++)
				if (List[i] is TTarget target)
					return target;

			return null;
		}

		public List<TTarget> FindAll<TTarget>()
			where TTarget : class
		{
			ValidateQuery<TTarget>();

			var selection = new List<TTarget>();

			for (int i = 0; i < List.Count; i++)
				if (List[i] is TTarget target)
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
		#endregion

		public TTarget Depend<TTarget>()
			where TTarget : class
		{
			var target = Find<TTarget>();

			if (target == null)
				throw new NullReferenceException($"No Component of Type {typeof(TTarget)} found in {this}");

			return target;
		}

		public GenericModularCollection()
		{
			List = new List<TItem>();
		}
	}
}