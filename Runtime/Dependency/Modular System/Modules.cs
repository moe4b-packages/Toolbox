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
	public interface IModules : IGenericModularCollection
	{
		void Register(IBehaviours behaviours);
		void Register(IBehaviours behaviours, ModuleScope scope);

		void Set();
	}

	[Serializable]
	public abstract class Modules<TReference, TModule> : GenericModularCollection<TModule>, IModules
		where TReference : Component
		where TModule : class, IModule<TReference>
	{
		public TReference Reference { get; protected set; }

		public virtual void Register(IBehaviours behaviours) => Register(behaviours, ModuleScope.Local);
		public virtual void Register(IBehaviours behaviours, ModuleScope scope)
		{
			var selection = behaviours.FindAll<TModule>();

			for (int i = 0; i < selection.Count; i++)
				if (ValidateScope(Reference, selection[i], scope))
					Add(selection[i]);
		}

		public void Set()
		{
			for (int i = 0; i < List.Count; i++)
				List[i].Set(Reference);
		}

		public Modules(TReference reference) : base()
		{
			this.Reference = reference;
		}

		public static bool ValidateScope(TReference reference, TModule module, ModuleScope scope)
		{
			switch (scope)
			{
				case ModuleScope.Local:
					return module.Context.IsChildOf(reference.transform);

				case ModuleScope.Global:
					return true;
			}

			throw new NotImplementedException();
		}
	}

	[Serializable]
	public class Modules<TReference> : Modules<TReference, IModule<TReference>>
		where TReference : Component
	{
		public Modules(TReference reference) : base(reference) { }
	}

	public enum ModuleScope
	{
		Local, Global
	}

	public interface IModule<TReference>
	{
		Transform Context { get; }

		void Set(TReference reference);
	}
}