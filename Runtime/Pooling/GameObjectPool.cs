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

		public abstract void Prewarm(int count);

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
			T instance;

			if (Stack.Count > 0)
			{
				instance = Stack.Pop();
				Enable(instance);
			}
			else
			{
				instance = Spawn();
			}

			if (instance is ICallback pool) pool.OnLease();
	        
			return instance;
		}

		void Enable(T instance)
        {
			instance.gameObject.SetActive(true);
		}

		int iterations;
		T Spawn()
		{
			iterations += 1;

			var gameObject = Object.Instantiate(prefab);
			gameObject.name = $"{prefab.name} ({iterations})";

			var instance = gameObject.GetComponent<T>();

			if (instance is IInitialize<T> target) target.Initialize(this);

			return instance;
		}

		public override void Prewarm(int count)
		{
			for (int i = 0; i < count; i++)
			{
				var item = Spawn();
				Disable(item);
				Stack.Push(item);
			}
		}

		public void Return(T instance)
		{
			if(instance is ICallback pool) pool.OnReturn();

			Disable(instance);

			Stack.Push(instance);
		}

		void Disable(T instance)
		{
			instance.gameObject.SetActive(false);
		}

		public GameObjectPool()
		{
			Stack = new Stack<T>();
		}
	}
}