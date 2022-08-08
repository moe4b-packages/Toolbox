using UnityEngine;

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;

using System;

namespace MB
{
	public static class MonobehaviourCallback
	{
		public static T Retrieve<T>(UObjectSurrogate target)
				where T : Component
		{
			if (target.GameObject.TryGetComponent<T>(out var component) == false)
				component = target.GameObject.AddComponent<T>();

			return component;
		}

		public abstract class Processor<T> : MonoBehaviour
			where T : Processor<T>
		{
			public static T Retrieve(UObjectSurrogate target) => Retrieve<T>(target);
		}
		public abstract class ActionProcessor<T> : Processor<T>
			where T : ActionProcessor<T>
		{
			public event Action Event;
			protected virtual void Invoke()
			{
				Event?.Invoke();
			}

			public void Register(Action callback) => Event += callback;
			public void Unregister(Action callback) => Event -= callback;
		}

		public class GameObject
		{
			public class State : Processor<State>
            {
				public event Action EnableEvent;
				void OnEnable() => EnableEvent?.Invoke();

				public event Action DisableEvent;
				void OnDisable() => DisableEvent?.Invoke();

				public event Action DestroyEvent;
				void OnDestroy() => DestroyEvent?.Invoke();
			}

			public class Destroy : ActionProcessor<Destroy>
			{
				void OnDestroy() => Invoke();
			}
			public class Enable : ActionProcessor<Enable>
			{
				void OnEnable() => Invoke();
			}
			public class Disable : ActionProcessor<Disable>
			{
				void OnDisable() => Invoke();
			}
		}

		public class Rigidbody
		{
			public class Collision : Processor<Collision>
			{
				public delegate void EventDelegate(Collision collision);

				public event EventDelegate EnterEvent;
				void OnCollisionEnter(Collision collision)
				{
					EnterEvent?.Invoke(collision);
				}

				public event EventDelegate StayEvent;
				void OnCollisionStay(Collision collision)
				{
					StayEvent?.Invoke(collision);
				}

				public event EventDelegate ExitEvent;
				void OnCollisionExit(Collision collision)
				{
					ExitEvent?.Invoke(collision);
				}
			}
			public class Trigger : Processor<Trigger>
			{
				public delegate void EventDelegate(Collider collider);

				public event EventDelegate EnterEvent;
				void OnTriggerEnter(Collider collider)
				{
					EnterEvent?.Invoke(collider);
				}

				public event EventDelegate StayEvent;
				void OnTriggerStay(Collider collider)
				{
					StayEvent?.Invoke(collider);
				}

				public event EventDelegate ExitEvent;
				void OnTriggerExit(Collider collider)
				{
					ExitEvent?.Invoke(collider);
				}
			}
		}
	}
}