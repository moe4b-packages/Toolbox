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
	public class AutoComponentExample : MonoBehaviour
	{
		[SerializeField]
		AutoComponent<Collider> auto_collider = AutoComponent.Self;
		public Collider Collider => auto_collider.Retrieve(this);

		[SerializeField]
		AutoComponents<Collider> auto_colliders = AutoComponents.Self | AutoComponents.Children | AutoComponents.Parents;
		public List<Collider> Colliders => auto_colliders.Retrieve(this);

		[SerializeField]
		AutoComponent<IAutoComponentSample> auto_sample = AutoComponent.Self;
		public IAutoComponentSample Sample => auto_sample.Retrieve(this);

		[SerializeField]
		AutoComponents<IAutoComponentSample> auto_samples = AutoComponents.Self | AutoComponents.Children | AutoComponents.Parents;
		public List<IAutoComponentSample> Samples => auto_samples.Retrieve(this);

		[SerializeField]
		AutoComponents<Component> auto_components = AutoComponents.Global;
		public List<Component> Components => auto_components.Retrieve(this);

		void Start()
		{
			Debug.Log(Collider);
			foreach (var item in Samples)
				Debug.Log(Colliders);

			Debug.Log(Sample);
			foreach (var item in Samples)
				Debug.Log(item);

			foreach (var item in Components)
				Debug.Log(item);
		}
	}
}