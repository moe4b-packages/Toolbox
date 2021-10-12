using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MB
{
    [AddComponentMenu(Paths.Variants + "UI Click Execution Relay")]
    public class UIClickExecutionRelay : ExecutionRelay, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData) => Invoke();
    }
}