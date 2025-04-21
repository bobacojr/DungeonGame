using UnityEngine;

public class AttackExitBehaviour : StateMachineBehaviour
{
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var pc = animator.GetComponent<PlayerController>();
        if (pc != null) pc.OnAttackComplete();
    }
}
