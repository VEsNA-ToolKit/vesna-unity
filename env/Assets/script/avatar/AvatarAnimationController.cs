using UnityEngine;
using UnityEngine.AI;

public class AvatarAnimationController : MonoBehaviour
{
    private Animator animator;
    [Range(0f, 3f)]
    public float walkingSpeed = 1.0f;
    private NavMeshAgent nav;

    void Awake()
    {
        animator = GetComponent<Animator>();
        nav = GetComponentInParent<NavMeshAgent>();

        SetSpeed();
    }
    public void SetAnimationState(string state)
    {
        switch (state)
        {
            case "walk":
                animator.SetFloat("walkSpeed", walkingSpeed);
                if (walkingSpeed < 3)
                {
                    animator.SetTrigger("Walk");


                }
                else
                {
                    animator.SetTrigger("Run");
                    animator.SetBool("isRunning", true);
                }

                break;
            case "stop":
                animator.SetTrigger("Idle");
                animator.SetFloat("walkSpeed", 0f);
                break;
            case "say":
                animator.SetTrigger("Talk");
                break;
        }
    }
    public void SetSpeed()
    {
        if (nav != null)
        {
            nav.speed = walkingSpeed + 1.0f;
        }
    }


}
