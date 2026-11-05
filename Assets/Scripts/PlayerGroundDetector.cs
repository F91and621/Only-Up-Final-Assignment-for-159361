using UnityEngine;

// Ground checks and landing score.
[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PlayerGroundDetector : MonoBehaviour
{
    public float minimumGroundNormalY = 0.4f;

    private Rigidbody playerRigidbody;
    private Collider playerCollider;

    public bool IsGrounded { get; private set; }
    public float LastGroundedTime { get; private set; } = float.NegativeInfinity;

    void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
    }

    void OnValidate()
    {
        minimumGroundNormalY = Mathf.Clamp01(minimumGroundNormalY);
    }

    public void UpdateGroundedState()
    {
        if (playerRigidbody.linearVelocity.y > 0.1f || playerCollider == null)
        {
            IsGrounded = false;
            return;
        }

        Bounds bounds = playerCollider.bounds;
        Vector3 origin = new Vector3(bounds.center.x, bounds.min.y + 0.35f,
            bounds.center.z);
        float offset = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.5f;
        Vector3 sphereOrigin = new Vector3(bounds.center.x,
            bounds.min.y + 0.65f, bounds.center.z);
        float sphereRadius = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.7f;

        IsGrounded = HasGroundBelowSphere(sphereOrigin, sphereRadius)
            || HasGroundBelow(origin)
            || HasGroundBelow(origin + Vector3.forward * offset)
            || HasGroundBelow(origin + Vector3.back * offset)
            || HasGroundBelow(origin + Vector3.left * offset)
            || HasGroundBelow(origin + Vector3.right * offset);

        if (IsGrounded)
            LastGroundedTime = Time.time;
    }

    public void ClearGround()
    {
        IsGrounded = false;
        LastGroundedTime = float.NegativeInfinity;
    }

    bool HasGroundBelow(Vector3 origin)
    {
        return Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                0.55f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && !hit.collider.transform.IsChildOf(transform)
            && hit.normal.y >= minimumGroundNormalY;
    }

    bool HasGroundBelowSphere(Vector3 origin, float radius)
    {
        return Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit,
                0.55f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && !hit.collider.transform.IsChildOf(transform)
            && hit.normal.y >= minimumGroundNormalY;
    }

    void OnCollisionEnter(Collision collision)
    {
        CheckGroundContact(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        CheckGroundContact(collision);
    }

    void CheckGroundContact(Collision collision)
    {
        if (playerRigidbody.linearVelocity.y > 0.1f)
            return;

        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y < minimumGroundNormalY)
                continue;

            IsGrounded = true;
            LastGroundedTime = Time.time;
            ScoreObject scoreObject =
                collision.collider.GetComponentInParent<ScoreObject>();
            if (scoreObject != null)
                scoreObject.TryAwardPoint();
            return;
        }
    }
}
