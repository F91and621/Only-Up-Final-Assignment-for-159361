using UnityEngine;
using UnityEngine.InputSystem;

// Player movement.
[RequireComponent(typeof(Rigidbody))]
public class BeginnerPlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float turnSpeed = 10f;
    public float minimumMoveSpeed = 2f;
    public float maximumMoveSpeed = 8f;
    public float speedChangeRate = 12f;

    [Header("Jump")]
    public float jumpSpeed = 7f;
    public float shortJumpGravityMultiplier = 2.2f;
    public float fallGravityMultiplier = 1.8f;
    public float jumpBufferTime = 0.15f;
    public float airControlAcceleration = 16f;
    public float groundedGraceTime = 0.15f;

    [Header("Reset")]
    public Transform resetPoint;
    public Key resetKey = Key.R;

    private Rigidbody playerRigidbody;
    private Animator playerAnimator;
    private Camera mainCamera;
    private PlayerGroundDetector groundDetector;
    private PlayerAttackController attackController;
    private Vector3 moveDirection;
    private float lastJumpPressedTime = float.NegativeInfinity;
    private float currentMoveSpeed;
    private bool shouldReset;
    private float enemyBumpEndTime = float.NegativeInfinity;
    private float freezeEndTime = float.NegativeInfinity;
    private bool isFrozen;

    public bool IsGrounded => groundDetector != null && groundDetector.IsGrounded;
    public bool CanAttack => enabled && !isFrozen && IsGrounded;

    void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        playerRigidbody.constraints |= RigidbodyConstraints.FreezeRotation;
        playerAnimator = GetComponentInChildren<Animator>();
        mainCamera = Camera.main;
        groundDetector = GetComponent<PlayerGroundDetector>();
        attackController = GetComponent<PlayerAttackController>();
        currentMoveSpeed = Mathf.Clamp(moveSpeed, minimumMoveSpeed, maximumMoveSpeed);

        if (resetPoint == null)
        {
            GameObject point = GameObject.Find("ResetPoint");
            if (point == null)
                point = GameObject.Find("resetpoint");
            if (point != null)
                resetPoint = point.transform;
        }
    }

    void OnValidate()
    {
        minimumMoveSpeed = Mathf.Max(0f, minimumMoveSpeed);
        maximumMoveSpeed = Mathf.Max(minimumMoveSpeed, maximumMoveSpeed);
        moveSpeed = Mathf.Clamp(moveSpeed, minimumMoveSpeed, maximumMoveSpeed);
        speedChangeRate = Mathf.Max(0f, speedChangeRate);
        airControlAcceleration = Mathf.Max(0f, airControlAcceleration);
        shortJumpGravityMultiplier = Mathf.Max(1f, shortJumpGravityMultiplier);
        fallGravityMultiplier = Mathf.Max(1f, fallGravityMultiplier);
        jumpBufferTime = Mathf.Max(0f, jumpBufferTime);
    }

    void Update()
    {
        UpdateFreeze();
        if (isFrozen)
            return;

        ReadInput();
        UpdateAnimation();
    }

    void FixedUpdate()
    {
        if (shouldReset)
        {
            ResetPlayer();
            shouldReset = false;
            return;
        }

        if (isFrozen)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            return;
        }

        if (groundDetector != null)
            groundDetector.UpdateGroundedState();

        if (!IsAttacking() && CanJump()
            && Time.time - lastJumpPressedTime <= jumpBufferTime)
        {
            Jump();
            lastJumpPressedTime = float.NegativeInfinity;
        }

        if (Time.time >= enemyBumpEndTime)
            MovePlayer();

        if (!IsGrounded)
            ApplyExtraGravity();
    }

    void ReadInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        float horizontal = 0f;
        float vertical = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            horizontal = -1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            horizontal = 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            vertical = -1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            vertical = 1f;

        moveDirection = GetMoveDirection(horizontal, vertical);
        UpdateMoveSpeed(keyboard);

        if (keyboard.spaceKey.wasPressedThisFrame)
            lastJumpPressedTime = Time.time;
        if (keyboard[resetKey].wasPressedThisFrame && resetPoint != null)
            shouldReset = true;
    }

    Vector3 GetMoveDirection(float horizontal, float vertical)
    {
        if (mainCamera == null)
            return Vector3.zero;

        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;
        forward.y = 0f;
        right.y = 0f;
        return (forward.normalized * vertical + right.normalized * horizontal).normalized;
    }

    void UpdateMoveSpeed(Keyboard keyboard)
    {
        bool faster = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        bool slower = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
        float targetSpeed = moveSpeed;

        if (faster && !slower)
            targetSpeed = maximumMoveSpeed;
        else if (slower && !faster)
            targetSpeed = minimumMoveSpeed;

        currentMoveSpeed = Mathf.MoveTowards(currentMoveSpeed, targetSpeed,
            speedChangeRate * Time.deltaTime);
    }

    void MovePlayer()
    {
        Vector3 velocity = playerRigidbody.linearVelocity;
        if (IsAttacking())
        {
            velocity.x = 0f;
            velocity.z = 0f;
            playerRigidbody.linearVelocity = velocity;
            return;
        }

        if (IsGrounded)
        {
            velocity.x = moveDirection.x * currentMoveSpeed;
            velocity.z = moveDirection.z * currentMoveSpeed;
        }
        else if (moveDirection != Vector3.zero)
        {
            Vector3 targetVelocity = moveDirection * currentMoveSpeed;
            Vector3 horizontalVelocity = Vector3.MoveTowards(
                new Vector3(velocity.x, 0f, velocity.z), targetVelocity,
                airControlAcceleration * Time.fixedDeltaTime);
            velocity.x = horizontalVelocity.x;
            velocity.z = horizontalVelocity.z;
        }

        playerRigidbody.linearVelocity = velocity;
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            Quaternion nextRotation = Quaternion.Slerp(playerRigidbody.rotation,
                targetRotation, turnSpeed * Time.fixedDeltaTime);
            playerRigidbody.MoveRotation(nextRotation);
        }
    }

    void ApplyExtraGravity()
    {
        float verticalSpeed = playerRigidbody.linearVelocity.y;
        Keyboard keyboard = Keyboard.current;
        if (verticalSpeed > 0f && keyboard != null && !keyboard.spaceKey.isPressed)
            playerRigidbody.AddForce(Physics.gravity
                * (shortJumpGravityMultiplier - 1f), ForceMode.Acceleration);
        else if (verticalSpeed < 0f)
            playerRigidbody.AddForce(Physics.gravity
                * (fallGravityMultiplier - 1f), ForceMode.Acceleration);
    }

    void Jump()
    {
        Vector3 velocity = playerRigidbody.linearVelocity;
        if (moveDirection != Vector3.zero)
        {
            velocity.x = moveDirection.x * currentMoveSpeed;
            velocity.z = moveDirection.z * currentMoveSpeed;
        }
        velocity.y = jumpSpeed;
        playerRigidbody.linearVelocity = velocity;

        if (groundDetector != null)
            groundDetector.ClearGround();
        if (playerAnimator != null)
            playerAnimator.SetTrigger("Jump");
    }

    bool CanJump()
    {
        if (groundDetector == null)
            return false;
        return IsGrounded
            || Time.time - groundDetector.LastGroundedTime <= groundedGraceTime;
    }

    bool IsAttacking()
    {
        return attackController != null && attackController.IsAttacking;
    }

    public void SetResetPoint(Transform point)
    {
        if (point != null)
            resetPoint = point;
    }

    public void RespawnAtResetPoint()
    {
        if (resetPoint != null)
            shouldReset = true;
    }

    public void ApplyEnemyBump(Vector3 bumpVelocity, float controlDelay)
    {
        playerRigidbody.linearVelocity = bumpVelocity;
        if (groundDetector != null)
            groundDetector.ClearGround();
        enemyBumpEndTime = Time.time + controlDelay;
    }

    public void FreezeForSeconds(float seconds)
    {
        isFrozen = true;
        freezeEndTime = Time.time + seconds;
        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;
        if (playerAnimator != null)
            playerAnimator.speed = 0f;
    }

    void UpdateFreeze()
    {
        if (!isFrozen || Time.time < freezeEndTime)
            return;
        isFrozen = false;
        if (playerAnimator != null)
            playerAnimator.speed = 1f;
    }

    void ResetPlayer()
    {
        playerRigidbody.position = resetPoint.position;
        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;
        moveDirection = Vector3.zero;
        lastJumpPressedTime = float.NegativeInfinity;
        enemyBumpEndTime = float.NegativeInfinity;
        freezeEndTime = float.NegativeInfinity;
        isFrozen = false;

        if (groundDetector != null)
            groundDetector.ClearGround();
        if (attackController != null)
            attackController.CancelAttack();
        if (playerAnimator != null)
        {
            playerAnimator.speed = 1f;
            playerAnimator.ResetTrigger("Jump");
            playerAnimator.Play("Base Layer.Idle", 0, 0f);
        }
    }

    void UpdateAnimation()
    {
        if (playerAnimator == null)
            return;
        float movement = IsAttacking() ? 0f : moveDirection.magnitude;
        float animationSpeed = currentMoveSpeed / Mathf.Max(moveSpeed, 0.01f);
        playerAnimator.SetFloat("Speed", movement);
        playerAnimator.SetFloat("AnimationSpeed", animationSpeed);
        playerAnimator.SetBool("IsGrounded", IsGrounded);
    }
}
