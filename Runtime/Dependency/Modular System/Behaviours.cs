using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MB
{
	[Serializable]
	public class Behaviours<TReference> : Segments<TReference, IBehaviour<TReference>>
		where TReference : Component
	{
		public virtual void Configure()
        {
            for (int i = 0; i < components.Count; i++)
				(components[i] as IBehaviour<TReference>).Configure();
        }
		public virtual void Initialize()
        {
			for (int i = 0; i < components.Count; i++)
				(components[i] as IBehaviour<TReference>).Initialize();
		}

        public Behaviours(TReference reference) : base(reference)
        {
			Register(reference.gameObject);
        }
	}

	public interface IBehaviour<TReference>
		where TReference : Component
	{
		void Configure();
		void Initialize();
	}
}