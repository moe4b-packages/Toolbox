using UnityEngine;

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;

namespace MB
{
	public class OnDisableCallback : MonobehaviourCallback
	{
        void OnDisable() => Invoke();

        public static OnDisableCallback Retrieve(UObjectSurrogate target) => Retrieve<OnDisableCallback>(target);
    }
}