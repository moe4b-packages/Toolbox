using UnityEngine;

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;

namespace MB
{
    [AddComponentMenu(Path + "On Enable")]
    public class OnEnableCallback : MonobehaviourCallback
	{
        void OnEnable() => Invoke();

        public static OnEnableCallback Retrieve(UObjectSurrogate target) => Retrieve<OnEnableCallback>(target);
    }
}