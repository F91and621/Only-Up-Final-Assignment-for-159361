using UnityEngine;
using UnityEngine.AI;

// Patrol between two points, chase a nearby player, then return to patrol
[RequireComponent(typeof(NavMeshAgent))]
public class GuardEnemyAI : MonoBehaviour
{
    public Transform player;
    public Transform patrolPointA;
    public Transform patrolPointB;
    public float noticeDistance = 7f;
    public float loseDistance = 11f;
    public float sideBumpSpeed = 5f;
    public float sideBumpUpSpeed = 3f;
    public float sideBumpControlDelay = 0.25f;
    public float sideBumpCooldown = 0.75f;

    public enum GuardState { Patrol, Chase, Return }
    public GuardState currentState = GuardState.Patrol;

    private NavMeshAgent agent;
    private Transform nextPatrolPoint;
    private float lastSideBumpTime = float.NegativeInfinity;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (player == null || patrolPointA == null || patrolPointB == null)
        {
            Debug.LogWarning(name + ": Assign the Player and both Patrol Points.", this);
            enabled = false;
            return;
        }

        nextPatrolPoint = patrolPointA;
        SetDestination(nextPatrolPoint.position);
    }

    void Update()
    {
        if (!agent.isOnNavMesh)
            return;

        float playerDistance = Vector3.Distance(transform.position, player.position);
        if (currentState != GuardState.Chase && playerDistance <= noticeDistance)
            currentState = GuardState.Chase;

        if (currentState == GuardState.Chase)
        {
            if (playerDistance > loseDistance)
            {
                currentState = GuardState.Return;
                SetDestination(nextPatrolPoint.position);
                return;
            }

            SetDestination(player.position);
            return;
        }

        if (agent.pathPending || agent.remainingDistance > agent.stoppingDistance + 0.25f)
            return;

        if (currentState == GuardState.Return)
            currentState = GuardState.Patrol;

        nextPatrolPoint = nextPatrolPoint == patrolPointA ? patrolPointB : patrolPointA;
        SetDestination(nextPatrolPoint.position);
    }

    void SetDestination(Vector3 target)
    {
        if (agent.isOnNavMesh)
            agent.SetDestination(target);
    }

    void OnTriggerEnter(Collider other)
    {
        CheckPlayer(other);
    }

    void OnTriggerStay(Collider other)
    {
        CheckPlayer(other);
    }

    void CheckPlayer(Collider other)
    {
        BeginnerPlayerController playerController = other.GetComponentInParent<BeginnerPlayerController>();
        Rigidbody playerBody = other.attachedRigidbody;
        if (playerController == null || playerBody == null)
            return;

        if (Time.time - lastSideBumpTime < sideBumpCooldown)
            return;

        Vector3 away = playerBody.position - transform.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f)
            away = -transform.forward;

        Vector3 bumpVelocity = away.normalized * sideBumpSpeed;
        bumpVelocity.y = sideBumpUpSpeed;
        playerController.ApplyEnemyBump(bumpVelocity, sideBumpControlDelay);
        lastSideBumpTime = Time.time;
    }
}
