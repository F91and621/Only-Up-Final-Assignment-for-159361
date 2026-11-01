using UnityEngine;

// 在平台上往返巡逻；玩家从上方落到头顶时被消灭。
public class PatrolStompEnemy : MonoBehaviour
{
    public float patrolHalfWidth = 2.5f;
    public float patrolSpeed = 1.5f;
    public float edgeInset = 0.65f;
    public float headHeight = 1.2f;
    public float stompBounceSpeed = 6f;
    public float sideBumpSpeed = 5f;
    public float sideBumpUpSpeed = 3f;
    public float sideBumpControlDelay = 0.25f;
    public float sideBumpCooldown = 0.75f;
    public Transform visualRoot;

    private Vector3 startPosition;
    private Collider currentSurface;
    private Vector3 patrolAxis;
    private Quaternion visualStartRotation;
    private float moveDirection = 1f;
    private bool defeated;
    private float lastSideBumpTime = float.NegativeInfinity;

    void Start()
    {
        startPosition = transform.position;
        if (visualRoot != null)
            visualStartRotation = visualRoot.localRotation;

        FindSurfaceBelow();

        patrolAxis = Vector3.ProjectOnPlane(
            currentSurface != null ? currentSurface.transform.right : transform.right,
            Vector3.up).normalized;
        if (patrolAxis.sqrMagnitude < 0.01f)
            patrolAxis = Vector3.right;

        if (TrySurfaceHit(transform.position, out RaycastHit hit))
        {
            Vector3 position = transform.position;
            position.y = hit.point.y;
            transform.position = position;
            startPosition = position;
        }
    }

    void FixedUpdate()
    {
        if (defeated || currentSurface == null)
            return;

        if (!TryMove(moveDirection, out Vector3 nextPosition))
        {
            moveDirection = -moveDirection;
            if (!TryMove(moveDirection, out nextPosition))
                return;
        }

        transform.position = nextPosition;

        if (visualRoot != null)
        {
            Vector3 localDirection = transform.InverseTransformDirection(patrolAxis * moveDirection);
            Quaternion targetRotation = Quaternion.FromToRotation(Vector3.right, localDirection)
                * visualStartRotation;
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation,
                targetRotation, Time.fixedDeltaTime * 8f);
        }
    }

    void FindSurfaceBelow()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 3f, Vector3.down);
        float closestDistance = float.PositiveInfinity;

        foreach (RaycastHit hit in Physics.RaycastAll(ray, 10f))
        {
            if (hit.collider.isTrigger || hit.collider.transform.IsChildOf(transform)
                || hit.normal.y < 0.55f || hit.point.y > transform.position.y + 2f)
                continue;

            float distance = Mathf.Abs(transform.position.y - hit.point.y);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                currentSurface = hit.collider;
            }
        }
    }

    bool TrySurfaceHit(Vector3 position, out RaycastHit hit)
    {
        if (currentSurface == null)
        {
            hit = default;
            return false;
        }

        Ray ray = new Ray(position + Vector3.up * 3f, Vector3.down);
        return currentSurface.Raycast(ray, out hit, 10f) && hit.normal.y >= 0.55f;
    }

    bool TryMove(float direction, out Vector3 nextPosition)
    {
        nextPosition = transform.position + patrolAxis * (direction * patrolSpeed * Time.fixedDeltaTime);

        if (patrolHalfWidth > 0f && Mathf.Abs(Vector3.Dot(nextPosition - startPosition, patrolAxis))
            > patrolHalfWidth)
            return false;

        if (currentSurface != null)
        {
            Bounds bounds = currentSurface.bounds;
            float margin = Mathf.Min(edgeInset,
                Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.4f);
            Vector3 edgeProbe = nextPosition + patrolAxis * (direction * margin);
            bool startInside = startPosition.x >= bounds.min.x && startPosition.x <= bounds.max.x
                && startPosition.z >= bounds.min.z && startPosition.z <= bounds.max.z;
            if (startInside && (edgeProbe.x < bounds.min.x || edgeProbe.x > bounds.max.x
                || edgeProbe.z < bounds.min.z || edgeProbe.z > bounds.max.z))
                return false;

            // 桌腿也属于同一个网格碰撞体；走到桌面边缘时不能落到桌腿上。
            if (!TrySurfaceHit(nextPosition, out RaycastHit groundHit)
                || Mathf.Abs(groundHit.point.y - transform.position.y) > 0.35f)
                return false;

            if (!TrySurfaceHit(edgeProbe, out RaycastHit edgeHit)
                || Mathf.Abs(edgeHit.point.y - groundHit.point.y) > 0.35f)
                return false;

            nextPosition.y = groundHit.point.y;
        }
        return true;
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
        if (player == null)
            return;

        Rigidbody playerBody = other.attachedRigidbody;
        if (playerBody == null)
            return;

        float headY = transform.position.y + headHeight;
        bool landedOnHead = playerBody.linearVelocity.y <= 0.15f
            && other.bounds.min.y >= headY - 0.65f;

        if (landedOnHead)
        {
            defeated = true;
            Vector3 velocity = playerBody.linearVelocity;
            velocity.y = stompBounceSpeed;
            playerBody.linearVelocity = velocity;

            if (PlayerProgressUI.Instance != null)
                PlayerProgressUI.Instance.AddPoint();

            Destroy(gameObject);
        }
        else if (Time.time - lastSideBumpTime >= sideBumpCooldown)
        {
            Vector3 away = playerBody.position - transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f)
                away = -patrolAxis * moveDirection;

            Vector3 bumpVelocity = away.normalized * sideBumpSpeed;
            bumpVelocity.y = sideBumpUpSpeed;
            player.ApplyEnemyBump(bumpVelocity, sideBumpControlDelay);
            lastSideBumpTime = Time.time;
        }
    }
}
