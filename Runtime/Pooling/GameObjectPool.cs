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
		/// Destroy All Pooled Objects
		/// </summary>
		public abstract void Clear();

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

		public List<T> All { get; }

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

			if (instance is ICallback callback) callback.OnLease();
	        
			return instance;
		}

		int iterations;
		T Spawn()
		{
			iterations += 1;

			var gameObject = Object.Instantiate(prefab);
			gameObject.name = $"{prefab.name} ({iterations})";

			var instance = gameObject.GetComponent<T>();

			All.Add(instance);

			if (instance is IInitialize<T> target) target.Initialize(this);

			return instance;
		}

		public void Return(T instance)
		{
			if(instance is ICallback calback) calback.OnReturn();

			Disable(instance);

			Stack.Push(instance);
		}

		public override void Prewarm(int count)
		{
			All.EnsureExtraCapacity(count);

			for (int i = 0; i < count; i++)
			{
				var item = Spawn();
				Disable(item);
				Stack.Push(item);
			}
		}

		void Enable(T instance)
		{
			instance.gameObject.SetActive(true);
		}
		void Disable(T instance)
		{
			instance.gameObject.SetActive(false);
		}

		public override void Clear()
		{
            for (int i = 0; i < All.Count; i++)
				Object.Destroy(All[i].gameObject);

			All.Clear();
			Stack.Clear();
		}

		public GameObjectPool()
		{
			Stack = new Stack<T>();
			All = new List<T>();
		}
		public GameObjectPool(GameObject prefab, int count)
        {
			this.prefab = prefab;

			Stack = new Stack<T>(count);
			All = new List<T>(count);

			Prewarm(count);
		}
	}
}