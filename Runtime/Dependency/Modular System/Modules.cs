using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MB
{
	[Serializable]
	public class Modules<TReference> : Segments<TReference, IModule<TReference>>
		where TReference : Component
	{
		public void Set()
		{
			for (int i = 0; i < components.Count; i++)
			{
				var target = components[i] as IModule<TReference>;
				target.Set(Reference);
			}
		}

        public Modules(TReference reference) : base(reference)
        {

        }
	}

    public interface IModule<TReference>
		where TReference : Component
    {
		void Set(TReference reference);
	}
}