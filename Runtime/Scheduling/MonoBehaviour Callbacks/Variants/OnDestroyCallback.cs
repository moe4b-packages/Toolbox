using UnityEngine;

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;

namespace MB
{
    [AddComponentMenu(Path + "On Destroy")]
	public class OnDestroyCallback : MonobehaviourCallback
	{
        void OnDestroy() => Invoke();

        public static OnDestroyCallback Retrieve(UObjectSurrogate target) => Retrieve<OnDestroyCallback>(target);
    }
}