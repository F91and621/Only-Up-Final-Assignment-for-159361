using UnityEngine;

// 小球每隔一段时间攻击玩家，然后返回原位。
public class AttackBallController : MonoBehaviour
{
    public float detectionRange = 50f;
    public float attackSpeed = 18f;
    public float returnSpeed = 22f;
    public float attackInterval = 10f;
    public float maximumAttackTime = 3f;
    public float freezeDuration = 1f;
    public float hitDistance = 1.2f;

    private Transform player;
    private Vector3 homePosition;
    private Vector3 targetPosition;
    private float attackTimer;
    private float waitTimer;

    // 0 = 等待，1 = 攻击，2 = 返回。
    private int state;

    void Start()
    {
        homePosition = transform.position;
        waitTimer = attackInterval;

        BeginnerPlayerController playerController =
            FindFirstObjectByType<BeginnerPlayerController>();

        if (playerController != null)
            player = playerController.transform;

    }

    void Update()
    {
        if (player == null)
            return;

        if (state == 0)
            WaitForAttack();
        else if (state == 1)
            MoveToTarget();
        else
            ReturnHome();
    }

    void WaitForAttack()
    {
        waitTimer -= Time.deltaTime;
        if (waitTimer > 0f)
            return;

        float distance = Vector3.Distance(homePosition, player.position);
        if (distance > detectionRange)
            return;

        targetPosition = player.position + Vector3.up * 1.2f;
        attackTimer = maximumAttackTime;
        state = 1;
    }

    void MoveToTarget()
    {
        targetPosition = player.position + Vector3.up * 1.2f;
        attackTimer -= Time.deltaTime;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            attackSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < hitDistance)
        {
            FreezePlayer();
            state = 2;
        }
        else if (attackTimer <= 0f)
            state = 2;
    }

    void ReturnHome()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            homePosition,
            returnSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, homePosition) < 0.05f)
        {
            transform.position = homePosition;
            waitTimer = attackInterval;
            state = 0;
        }
    }

    void FreezePlayer()
    {
        BeginnerPlayerController playerController =
            player.GetComponent<BeginnerPlayerController>();

        if (playerController != null)
            playerController.FreezeForSeconds(freezeDuration);
    }

}
