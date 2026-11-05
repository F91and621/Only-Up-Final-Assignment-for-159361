using UnityEngine;

// Moves between two points; a stomp from above defeats the enemy
public class PatrolStompEnemy : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public float patrolSpeed = 1.5f;
    public float headHeight = 1.2f;
    public float stompBounceSpeed = 6f;
    public float sideBumpSpeed = 5f;
    public float sideBumpUpSpeed = 3f;
    public float sideBumpControlDelay = 0.25f;
    public float sideBumpCooldown = 0.75f;
    public Transform visualRoot;

    private Transform target;
    private Quaternion visualStartRotation;
    private bool defeated;
    private float lastSideBumpTime = float.NegativeInfinity;

    void Start()
    {
        if (visualRoot != null)
            visualStartRotation = visualRoot.localRotation;
        target = pointB;
    }

    void FixedUpdate()
    {
        if (defeated || pointA == null || pointB == null)
            return;

        transform.position = Vector3.MoveTowards(transform.position,
            target.position, patrolSpeed * Time.fixedDeltaTime);
        if (Vector3.Distance(transform.position, target.position) < 0.05f)
            target = target == pointA ? pointB : pointA;

        if (visualRoot != null)
        {
            Vector3 localDirection = transform.InverseTransformDirection(
                target.position - transform.position);
            if (localDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.FromToRotation(Vector3.right,
                    localDirection) * visualStartRotation;
                visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation,
                    targetRotation, Time.fixedDeltaTime * 8f);
            }
        }
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
        if (defeated)
            return;

        BeginnerPlayerController player = other.GetComponentInParent<BeginnerPlayerController>();
        Rigidbody playerBody = other.attachedRigidbody;
        if (player == null || playerBody == null)
            return;

        float feetY = other.bounds.min.y;
        float fallSpeed = playerBody.linearVelocity.y;
        float previousFeetY = feetY - fallSpeed * Time.fixedDeltaTime;
        float headY = transform.position.y + headHeight;
        Collider enemyCollider = GetComponent<Collider>();

        // Estimate the previous foot height so a fast fall can still count as a stomp
        bool landedOnHead = enemyCollider != null && fallSpeed < -0.1f
            && previousFeetY >= headY - 0.2f
            && feetY >= enemyCollider.bounds.center.y;

        if (landedOnHead)
        {
            defeated = true;
            Vector3 velocity = playerBody.linearVelocity;
            velocity.y = stompBounceSpeed;
            playerBody.linearVelocity = velocity;

            if (PlayerProgressUI.Instance != null)
                PlayerProgressUI.Instance.AddPoint();

            Destroy(gameObject);
            return;
        }

        if (Time.time - lastSideBumpTime < sideBumpCooldown)
            return;

        Vector3 away = playerBody.position - transform.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f)
            away = -transform.right;

        Vector3 bumpVelocity = away.normalized * sideBumpSpeed;
        bumpVelocity.y = sideBumpUpSpeed;
        player.ApplyEnemyBump(bumpVelocity, sideBumpControlDelay);
        lastSideBumpTime = Time.time;
    }
}
