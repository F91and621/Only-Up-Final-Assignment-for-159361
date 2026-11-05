using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Player attack animation.
public class PlayerAttackController : MonoBehaviour
{
    public Key attackKey = Key.F;

    private BeginnerPlayerController playerController;
    private Animator playerAnimator;

    public bool IsAttacking { get; private set; }

    void Awake()
    {
        playerController = GetComponent<BeginnerPlayerController>();
        playerAnimator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (playerController == null || !playerController.CanAttack || IsAttacking)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[attackKey].wasPressedThisFrame)
            StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        if (playerAnimator == null)
            yield break;

        IsAttacking = true;
        playerAnimator.SetTrigger("Attack");
        yield return null;

        float enterDeadline = Time.time + 0.5f;
        while (!IsAttackState() && Time.time < enterDeadline)
            yield return null;

        if (!IsAttackState())
        {
            CancelAttack();
            yield break;
        }

        float finishDeadline = Time.time + 2f;
        while (IsAttackState() || playerAnimator.IsInTransition(0))
        {
            if (Time.time >= finishDeadline)
                break;
            yield return null;
        }
        IsAttacking = false;
    }

    bool IsAttackState()
    {
        return playerAnimator.GetCurrentAnimatorStateInfo(0)
            .IsName("Base Layer.Attack");
    }

    public void CancelAttack()
    {
        StopAllCoroutines();
        IsAttacking = false;
        if (playerAnimator != null)
            playerAnimator.ResetTrigger("Attack");
    }
}
