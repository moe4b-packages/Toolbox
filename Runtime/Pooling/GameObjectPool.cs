using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace MB
{
	/// <summary>
	/// A system to provide generic based pool-able game objects
	/// </summary>
	[Serializable]
	public abstract class GameObjectPool
	{
		[SerializeField]
		protected GameObject prefab;
		public GameObject Prefab => prefab;
		
		/// <summary>
		/// Implement on Pool Object to Receive Lease & Return Callbacks
		/// </summary>
		public interface ICallback
		{
			void OnLease();
			void OnReturn();
		}
		
		/// <summary>
		/// Implement on Pool Object to Recieve Initialize Callback when Spawning the Object
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public interface IInitialize<T>
			where T : Component
		{
			void Initialize(GameObjectPool<T> reference);
		}
	}
	
	[Serializable]
	public class GameObjectPool<T> : GameObjectPool
		where T: Component
	{
		public Stack<T> Stack { get; }

		public T Lease()
		{
			var instance = Stack.Count == 0 ? Spawn() : Stack.Pop();

			instance.gameObject.SetActive(true);
	        
			if(instance is ICallback pool) pool.OnLease();
	        
			return instance;
		}

		private int iterations;
		private T Spawn()
		{
			var instance = Object.Instantiate(prefab);
			instance.name = $"{prefab.name} ({iterations})";

			var script = instance.GetComponent<T>();

			if (script is IInitialize<T> target) target.Initialize(this);

			iterations += 1;

			return script;
		}

		public void Return(T instance)
		{
			if(instance is ICallback pool) pool.OnReturn();
	        
			instance.gameObject.SetActive(false);
	        
			Stack.Push(instance);
		}

		public GameObjectPool()
		{
			Stack = new Stack<T>();
		}
	}
}