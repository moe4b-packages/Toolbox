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

using UnityEngine.Animations;

namespace MB
{
    public class SmartStateMachineBehaviour : StateMachineBehaviour
    {
        public Animator Animator { get; protected set; }

        public int LayerIndex { get; protected set; }
        public string LayerName { get; protected set; }

        public float LayerWeight
        {
            get => Animator.GetLayerWeight(LayerIndex);
            set => Animator.SetLayerWeight(LayerIndex, value);
        }

        protected virtual void Prepare(Animator Animator, int LayerIndex)
        {
            if (this.Animator == Animator) return;

            this.Animator = Animator;
            this.LayerIndex = LayerIndex;

            LayerName = Animator.GetLayerName(LayerIndex);

            Init();
        }
        protected virtual void Init() { }

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            Prepare(animator, layerIndex);

            base.OnStateEnter(animator, stateInfo, layerIndex);
        }
    }
}