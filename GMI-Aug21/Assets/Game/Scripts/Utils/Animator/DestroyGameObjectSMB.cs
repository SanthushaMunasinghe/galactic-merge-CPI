using UnityEngine;

namespace Oxtail.Utils
{
    public class DestroyGameObjectSMB : StateMachineBehaviour
    {
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateExit(animator, stateInfo, layerIndex);

            Destroy(animator.gameObject);
        }
    }
}
