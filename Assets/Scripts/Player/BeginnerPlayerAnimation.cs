using UnityEngine;

// 只负责角色动画参数。
public class BeginnerPlayerAnimation : MonoBehaviour
{
    private Animator playerAnimator;

    void Awake()
    {
        playerAnimator = GetComponentInChildren<Animator>();
    }

    public void UpdateAnimation(Vector3 moveDirection, bool isGrounded)
    {
        if (playerAnimator == null)
            return;

        playerAnimator.SetFloat("Speed", moveDirection.magnitude);
        playerAnimator.SetBool("IsGrounded", isGrounded);
    }

    public void PlayJump()
    {
        if (playerAnimator != null)
            playerAnimator.SetTrigger("Jump");
    }

    public void SetFrozen(bool frozen)
    {
        if (playerAnimator != null)
            playerAnimator.speed = frozen ? 0f : 1f;
    }

    public void ResetAnimation()
    {
        if (playerAnimator == null)
            return;

        playerAnimator.ResetTrigger("Jump");
        playerAnimator.speed = 1f;
        playerAnimator.Play("Base Layer.Idle", 0, 0f);
    }
}
