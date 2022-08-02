using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MB
{
	/// <summary>
	/// System to provider Update calls (Process) based on interfaces instead of reflection,
	/// needs explicit adding and removing of entries
	/// </summary>
	[AddComponentMenu(Toolbox.Paths.Misc + "Behaviour Update Manager")]
	public class BehaviourUpdateManager : MonoBehaviour
	{
		public static BehaviourUpdateManager Component { get; }
		
		public static HashSet<ICallback> HashSet { get; }
		public static List<ICallback> List { get; }

		public static bool Add(ICallback callback)
		{
			if (HashSet.Add(callback) == false)
				return false;
			
			List.Add(callback);
			return true;
		}
		public static bool Remove(ICallback callback)
		{
			if (HashSet.Remove(callback) == false)
				return false;

			List.Clear();

			foreach (var item in HashSet)
				List.Add(item);
			
			return true;
		}
		
		public void Update()
		{
			for (var i = 0; i < List.Count; i++)
			{
				if(List[i].isActiveAndEnabled == false) continue;
				
				List[i].Process();
			}
		}

		static BehaviourUpdateManager()
		{
			if (Application.isPlaying == false) return;

			var gameObject = new GameObject("Behaviour Update Manager");
			DontDestroyOnLoad(gameObject);
			
			Component = gameObject.AddComponent<BehaviourUpdateManager>();

			HashSet = new HashSet<ICallback>();
			List = new List<ICallback>();
		}

		public interface ICallback
		{
			bool isActiveAndEnabled { get; }
			
			void Process();
		}
	}
}