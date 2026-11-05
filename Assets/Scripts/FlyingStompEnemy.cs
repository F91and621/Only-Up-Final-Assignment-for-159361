using UnityEngine;

// Flying stomp enemy.
[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class FlyingStompEnemy : MonoBehaviour
{
    public float flyHeight = 3f;
    public float cycleSeconds = 4f;
    public float headHeight = 1.2f;
    public float stompBounceSpeed = 9f;
    public float sideBumpSpeed = 10f;
    public float sideBumpUpSpeed = 3f;
    public float sideBumpControlDelay = 0.25f;
    public float sideBumpCooldown = 0.75f;

    private Rigidbody enemyBody;
    private Collider enemyCollider;
    private Vector3 startPosition;
    private bool defeated;
    private float lastSideBumpTime = float.NegativeInfinity;

    void Start()
    {
        enemyBody = GetComponent<Rigidbody>();
        enemyCollider = GetComponent<Collider>();
        startPosition = transform.localPosition;
    }

    void FixedUpdate()
    {
        if (defeated)
            return;

        Vector3 localPosition = startPosition;
        localPosition.y -= Mathf.Sin(Time.time * 2f * Mathf.PI
            / Mathf.Max(0.1f, cycleSeconds)) * flyHeight;
        Vector3 worldPosition = transform.parent == null
            ? localPosition : transform.parent.TransformPoint(localPosition);
        enemyBody.MovePosition(worldPosition);
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

        BeginnerPlayerController player =
            other.GetComponentInParent<BeginnerPlayerController>();
        Rigidbody playerBody = other.attachedRigidbody;
        if (player == null || playerBody == null)
            return;

        float feetY = other.bounds.min.y;
        float fallSpeed = playerBody.linearVelocity.y;
        float previousFeetY = feetY - fallSpeed * Time.fixedDeltaTime;
        float headY = transform.position.y + headHeight;
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
            away = transform.right;

        Vector3 bumpVelocity = away.normalized * sideBumpSpeed;
        bumpVelocity.y = sideBumpUpSpeed;
        player.ApplyEnemyBump(bumpVelocity, sideBumpControlDelay);
        lastSideBumpTime = Time.time;
    }
}
