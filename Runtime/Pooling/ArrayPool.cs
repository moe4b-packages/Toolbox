using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MB
{
	/// <summary>
	/// Array pooling system that returns arrays bigger than or equal to the requested length
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public static class ArrayPool<T>
	{
		static HashSet<T[]> Set;

		public static Handle Lease(int length, out T[] array)
		{
			array = Lease(length);

			return new Handle(array);
		}
		public class Handle : IDisposable
		{
			T[] array;

			public void Dispose()
			{
				Return(array);
			}

			public Handle(T[] array)
			{
				this.array = array;
			}
		}

		public static T[] Lease(int length)
		{
			foreach (var array in Set)
			{
				if (array.Length >= length)
				{
					Set.Remove(array);
					return array;
				}
			}

			return new T[length];
		}

		public static void Return(T[] array)
		{
			Set.Add(array);
		}

		static ArrayPool()
		{
			Set = new HashSet<T[]>();
		}
	}
}