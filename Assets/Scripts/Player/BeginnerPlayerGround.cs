using UnityEngine;

// 负责落地检测和自动上台阶。
[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class BeginnerPlayerGround : MonoBehaviour
{
    [Header("Ground")]
    public float minimumGroundNormalY = 0.4f;
    public float groundCheckDistance = 0.55f;

    [Header("Step")]
    public float stepHeight = 0.5f;
    public float stepCheckDistance = 0.35f;

    public bool IsGrounded { get; private set; }

    private Rigidbody playerRigidbody;
    private Collider playerCollider;

    void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
    }

    public void CheckGround()
    {
        if (playerRigidbody.linearVelocity.y > 0.1f)
        {
            IsGrounded = false;
            return;
        }

        Bounds bounds = playerCollider.bounds;
        Vector3 center = new Vector3(
            bounds.center.x,
            bounds.min.y + 0.35f,
            bounds.center.z);

        float offset = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.5f;
        Vector3[] points =
        {
            center,
            center + Vector3.forward * offset,
            center + Vector3.back * offset,
            center + Vector3.left * offset,
            center + Vector3.right * offset
        };

        IsGrounded = false;
        for (int i = 0; i < points.Length; i++)
        {
            if (HasGroundBelow(points[i]))
            {
                IsGrounded = true;
                break;
            }
        }
    }

    bool HasGroundBelow(Vector3 origin)
    {
        RaycastHit hit;
        bool foundGround = FindRayHit(
            origin, Vector3.down, groundCheckDistance, out hit);

        return foundGround
            && !hit.collider.transform.IsChildOf(transform)
            && hit.normal.y >= minimumGroundNormalY;
    }

    public void TryStepUp(Vector3 moveDirection)
    {
        if (!IsGrounded || moveDirection == Vector3.zero)
            return;

        if (Mathf.Abs(playerRigidbody.linearVelocity.y) > 0.1f)
            return;

        Bounds bounds = playerCollider.bounds;
        float radius = Mathf.Min(bounds.extents.x, bounds.extents.z);

        Vector3 lowerPoint = new Vector3(
            bounds.center.x,
            bounds.min.y + 0.1f,
            bounds.center.z);

        lowerPoint += moveDirection * (radius + 0.02f);

        if (!RayHitsSomething(lowerPoint, moveDirection, stepCheckDistance))
            return;

        Vector3 upperPoint = lowerPoint + Vector3.up * (stepHeight + 0.05f);
        if (RayHitsSomething(upperPoint, moveDirection, stepCheckDistance))
            return;

        Vector3 downPoint = upperPoint + moveDirection * stepCheckDistance;
        RaycastHit stepHit;
        bool foundTop = FindRayHit(
            downPoint, Vector3.down, stepHeight + 0.1f, out stepHit);

        if (!foundTop || stepHit.normal.y < minimumGroundNormalY)
            return;

        float height = stepHit.point.y - bounds.min.y;
        if (height <= 0.02f || height > stepHeight)
            return;

        playerRigidbody.MovePosition(
            playerRigidbody.position + Vector3.up * height);
    }

    bool RayHitsSomething(Vector3 origin, Vector3 direction, float distance)
    {
        return Physics.Raycast(
            origin,
            direction,
            distance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);
    }

    bool FindRayHit(Vector3 origin, Vector3 direction,
        float distance, out RaycastHit hit)
    {
        return Physics.Raycast(
            origin,
            direction,
            out hit,
            distance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);
    }

    public void ResetGround()
    {
        IsGrounded = false;
    }
}
