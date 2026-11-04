using UnityEngine;

// Flies up and down; stomping it gives the player a boost.
public class FlyingStompEnemy : MonoBehaviour
{
    public float flyHeight = 3f;
    public float cycleSeconds = 4f;
    public float phase;
    public float headHeight = 1.2f;
    public float stompBounceSpeed = 9f;
    public float sideBumpSpeed = 10f;
    public float sideBumpUpSpeed = 3f;
    public float sideBumpControlDelay = 0.25f;
    public float sideBumpCooldown = 0.75f;

    Vector3 startPosition;
    bool defeated;
    float lastSideBumpTime = float.NegativeInfinity;

    void Start()
    {
        startPosition = transform.localPosition;
    }

    void Update()
    {
        if (defeated)
            return;

        Vector3 position = startPosition;
        position.y += Mathf.Sin(Time.time * 2f * Mathf.PI / Mathf.Max(0.1f, cycleSeconds)
            + phase) * flyHeight;
        transform.localPosition = position;
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

        bool landedOnHead = playerBody.linearVelocity.y <= 0.15f
            && other.bounds.min.y >= transform.position.y + headHeight - 0.65f;

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
                away = transform.right;

            Vector3 bumpVelocity = away.normalized * sideBumpSpeed;
            bumpVelocity.y = sideBumpUpSpeed;
            player.ApplyEnemyBump(bumpVelocity, sideBumpControlDelay);
            lastSideBumpTime = Time.time;
        }
    }
}
