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
	public interface IBehaviours : IGenericModularCollection
	{

	}

	[Serializable]
	public abstract class Behaviours<TReference, TBehaviour> : GenericModularCollection<TBehaviour>, IBehaviours
		where TReference : Component
		where TBehaviour : IBehaviour<TReference>
	{
		public TReference Reference { get; protected set; }

		public virtual void Configure()
		{
			for (int i = 0; i < List.Count; i++)
				List[i].Configure();
		}

		public virtual void Init()
		{
			for (int i = 0; i < List.Count; i++)
				List[i].Init();
		}

		public Behaviours(TReference reference) : base()
		{
			var selection = reference.GetComponentsInChildren<TBehaviour>(true);

			AddAll(selection);
		}
	}

	[Serializable]
	public class Behaviours<TReference> : Behaviours<TReference, IBehaviour<TReference>>
		where TReference : Component
	{
		public Behaviours(TReference reference) : base(reference) { }
	}

	public interface IBehaviour<TReference>
	{
		void Configure();

		void Init();
	}
}