using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MB
{
	/// <summary>
	/// A list that will not allocate memeory for storing up to 2 items, but will start allocating after that
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public struct MinAllocList<T> : IEnumerable<T>
	{
		T First;
		T Second;

		List<T> List;
		void EnsureList()
		{
			if (List == null) List = new List<T>();
		}

		public int Count { get; private set; }

		public T this[int index]
		{
			get
			{
				if (index >= Count)
					throw new ArgumentOutOfRangeException("index", Count, "Index out of Count Range");

				switch (index)
				{
					case 0:
						return First;

					case 1:
						return Second;

					default:
						return List[index - 2];
				}

				throw new NotImplementedException();
			}
			set
			{
				if (index >= Count)
					throw new ArgumentOutOfRangeException("index", Count, "Index out of Count Range");

				switch (index)
				{
					case 0:
						First = value;
						break;

					case 1:
						Second = value;
						break;

					default:
						List[index - 2] = value;
						break;
				}
			}
		}

		public void Add(T item)
		{
			switch (Count)
			{
				case 0:
					First = item;
					break;

				case 1:
					Second = item;
					break;

				case 2:
					EnsureList();
					List.Add(item);
					break;

				default:
					List.Add(item);
					break;
			}

			Count += 1;
		}

		public bool Contains(T item)
		{
			for (int i = 0; i < Count; i++)
				if (Equals(item, this[i]))
					return true;

			return false;
		}

		public int IndexOf(T item)
		{
			for (int i = 0; i < Count; i++)
				if (Equals(item, this[i]))
					return i;

			return -1;
		}

		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 0; i < Count; i++)
				yield return this[i];
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			for (int i = 0; i < Count; i++)
				yield return this[i];
		}
	}
}