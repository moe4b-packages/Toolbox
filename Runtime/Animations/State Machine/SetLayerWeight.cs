using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MB
{
    [AddComponentMenu(Toolbox.Path + "Set Layer Weight")]
    public class SetLayerWeight : SmartStateMachineBehaviour
    {
        [Range(0f, 1f)]
        float target = 1f;
        public float Target
        {
            get => target;
            set => target = value;
        }

        [SerializeField]
        float speed = 5f;
        public float Speed
        {
            get => speed;
            set => speed = value;
        }

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateEnter(animator, stateInfo, layerIndex);

            Process();
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateUpdate(animator, stateInfo, layerIndex);

            Process();
        }

        void Process()
        {
            if (LayerWeight == target) return;

            LayerWeight = Mathf.MoveTowards(LayerWeight, target, speed * Time.deltaTime);
            Animator.SetLayerWeight(LayerIndex, LayerWeight);
        }
    }
}
