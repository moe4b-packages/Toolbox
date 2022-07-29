using UnityEngine;

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;

using System;

namespace MB
{
	public abstract class MonobehaviourCallback : MonoBehaviour
	{
		public event Action Event;
		protected virtual void Invoke()
        {
			Event?.Invoke();
        }

		public void Register(Action callback) => Event += callback;
		public void Unregister(Action callback) => Event -= callback;

		public static T Retrieve<T>(UObjectSurrogate target)
			where T : MonobehaviourCallback
		{
			if (target.GameObject.TryGetComponent<T>(out var component) == false)
				component = target.GameObject.AddComponent<T>();

			return component;
		}
	}
}