using UnityEngine;

// 处理 Rigidbody 的移动、跳跃和下落。
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BeginnerPlayerGround))]
public class BeginnerPlayerMovement : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 5f;
    public float turnSpeed = 10f;

    [Header("Jump")]
    public float jumpSpeed = 7f;
    public float fallGravityMultiplier = 1.8f;
    public float maximumFallSpeed = 40f;

    public Vector3 MoveDirection { get; private set; }
    public bool IsGrounded
    {
        get { return playerGround.IsGrounded; }
    }

    private Rigidbody playerRigidbody;
    private BeginnerPlayerGround playerGround;
    private Camera movementCamera;

    void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        playerGround = GetComponent<BeginnerPlayerGround>();
    }

    void Start()
    {
        playerRigidbody.maxLinearVelocity = maximumFallSpeed;
        FindMovementCamera();
    }

    public bool Move(float horizontalInput, float verticalInput,
        bool jumpRequested)
    {
        playerGround.CheckGround();
        MoveDirection = GetMoveDirection(horizontalInput, verticalInput);

        if (!IsGrounded && playerRigidbody.linearVelocity.y < -0.1f)
        {
            FallStraightDown();
            return false;
        }

        MovePlayer();
        playerGround.TryStepUp(MoveDirection);

        if (jumpRequested && IsGrounded)
        {
            Jump();
            return true;
        }

        ApplyFallGravity();
        return false;
    }

    void FindMovementCamera()
    {
        BeginnerCameraController cameraController =
            FindFirstObjectByType<BeginnerCameraController>();

        if (cameraController != null)
            movementCamera = cameraController.GetComponent<Camera>();

        if (movementCamera == null)
            movementCamera = Camera.main;
    }

    Vector3 GetMoveDirection(float horizontalInput, float verticalInput)
    {
        if (movementCamera == null)
            return Vector3.zero;

        Vector3 forward = movementCamera.transform.forward;
        Vector3 right = movementCamera.transform.right;
        forward.y = 0f;
        right.y = 0f;

        Vector3 direction = forward.normalized * verticalInput
            + right.normalized * horizontalInput;

        return direction.normalized;
    }

    void MovePlayer()
    {
        Vector3 velocity = playerRigidbody.linearVelocity;
        velocity.x = MoveDirection.x * moveSpeed;
        velocity.z = MoveDirection.z * moveSpeed;
        playerRigidbody.linearVelocity = velocity;

        if (MoveDirection == Vector3.zero)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(MoveDirection);
        Quaternion nextRotation = Quaternion.Slerp(
            playerRigidbody.rotation,
            targetRotation,
            turnSpeed * Time.fixedDeltaTime);

        playerRigidbody.MoveRotation(nextRotation);
    }

    void FallStraightDown()
    {
        MoveDirection = Vector3.zero;

        Vector3 velocity = playerRigidbody.linearVelocity;
        velocity.x = 0f;
        velocity.z = 0f;
        playerRigidbody.linearVelocity = velocity;

        ApplyFallGravity();
    }

    void Jump()
    {
        Vector3 velocity = playerRigidbody.linearVelocity;
        velocity.y = jumpSpeed;
        playerRigidbody.linearVelocity = velocity;
        playerGround.ResetGround();
    }

    void ApplyFallGravity()
    {
        if (IsGrounded || playerRigidbody.linearVelocity.y >= 0f)
            return;

        Vector3 extraGravity =
            Physics.gravity * (fallGravityMultiplier - 1f);

        playerRigidbody.AddForce(extraGravity, ForceMode.Acceleration);
    }

    public void StopMovement()
    {
        MoveDirection = Vector3.zero;
    }

    public void ResetMovement()
    {
        MoveDirection = Vector3.zero;
        playerGround.ResetGround();
    }
}
