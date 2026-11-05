using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Controls player movement, jumping, and animation.
[RequireComponent(typeof(Rigidbody))]
public class BeginnerPlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float turnSpeed = 10f;

    [Header("Speed Control")]
    public float minimumMoveSpeed = 2f;
    public float maximumMoveSpeed = 8f;
    public float speedChangeRate = 12f;

    [Header("Jumping")]
    public float jumpSpeed = 7f;
    [Tooltip("Gravity multiplier while rising after the jump key is released. Tap for a short jump or hold for full height.")]
    public float shortJumpGravityMultiplier = 2.2f;
    [Tooltip("Gravity multiplier while falling. This shortens the fall without changing jump height.")]
    public float fallGravityMultiplier = 1.8f;
    [Tooltip("Time that a jump input remains buffered before landing.")]
    public float jumpBufferTime = 0.15f;
    public float minimumGroundNormalY = 0.4f;
    [Tooltip("Horizontal acceleration while airborne, allowing the player to move away from obstacle edges.")]
    public float airControlAcceleration = 16f;
    [Tooltip("Short grace period during which the player can still jump after leaving uneven ground.")]
    public float groundedGraceTime = 0.15f;

    [Header("Attack")]
    public Key attackKey = Key.F;

    [Header("Reset")]
    public Transform resetPoint;
    [Tooltip("Input Actions asset containing the Player/Reset action.")]
    public InputActionAsset playerActions;
    public Key resetKey = Key.R;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [Header("Test Flight (Editor / Development Build Only)")]
    [Tooltip("Press this key to toggle test flight mode.")]
    public Key flightToggleKey = Key.F2;
    [Tooltip("Movement speed in flight. Use Shift to rise, Ctrl to descend, and WASD to move horizontally.")]
    public float flightSpeed = 12f;
#endif

    private Rigidbody playerRigidbody;
    private Collider playerCollider;
    private Animator playerAnimator;
    private Camera mainCamera;

    private Vector3 moveDirection;
    private bool isGrounded;
    private bool isAttacking;
    private float lastGroundedTime = float.NegativeInfinity;
    private float lastJumpPressedTime = float.NegativeInfinity;
    private float currentMoveSpeed;
    private InputAction resetAction;
    private bool shouldReset;
    private float enemyBumpEndTime = float.NegativeInfinity;
    private float freezeEndTime = float.NegativeInfinity;
    private bool isFrozen;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private bool isFlightEnabled;
    private bool gravityWasEnabled;
    private float flightVerticalInput;
#endif

    void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
        // Collisions can apply torque to the rigidbody, so physics rotation is locked
        // while character turning remains controlled explicitly by MoveRotation.
        playerRigidbody.constraints |= RigidbodyConstraints.FreezeRotation;
        playerAnimator = GetComponentInChildren<Animator>();
        mainCamera = Camera.main;
        currentMoveSpeed = Mathf.Clamp(moveSpeed, minimumMoveSpeed, maximumMoveSpeed);

        if (resetPoint == null)
        {
            GameObject point = GameObject.Find("ResetPoint");
            if (point == null)
                point = GameObject.Find("resetpoint");

            if (point != null)
                resetPoint = point.transform;
        }

        if (playerActions != null)
        {
            resetAction = playerActions.FindAction("Player/Reset");
            if (resetAction != null)
                resetAction.Enable();
        }
    }

    void OnDestroy()
    {
        if (resetAction != null)
            resetAction.Disable();
    }

    void OnValidate()
    {
        minimumMoveSpeed = Mathf.Max(0f, minimumMoveSpeed);
        maximumMoveSpeed = Mathf.Max(minimumMoveSpeed, maximumMoveSpeed);
        moveSpeed = Mathf.Clamp(moveSpeed, minimumMoveSpeed, maximumMoveSpeed);
        speedChangeRate = Mathf.Max(0f, speedChangeRate);
        airControlAcceleration = Mathf.Max(0f, airControlAcceleration);
        minimumGroundNormalY = Mathf.Clamp01(minimumGroundNormalY);
        shortJumpGravityMultiplier = Mathf.Max(1f, shortJumpGravityMultiplier);
        fallGravityMultiplier = Mathf.Max(1f, fallGravityMultiplier);
        jumpBufferTime = Mathf.Max(0f, jumpBufferTime);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        flightSpeed = Mathf.Max(0f, flightSpeed);
#endif
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (isFlightEnabled)
        {
            MoveFlight();
            return;
        }
#endif

        UpdateGroundedState();
        if (!isAttacking && CanJump()
            && Time.time - lastJumpPressedTime <= jumpBufferTime)
        {
            Jump();
            lastJumpPressedTime = float.NegativeInfinity;
        }

        if (Time.time >= enemyBumpEndTime)
            MovePlayer();

        if (!isGrounded)
        {
            float verticalSpeed = playerRigidbody.linearVelocity.y;
            Keyboard keyboard = Keyboard.current;
            if (verticalSpeed > 0f && keyboard != null && !keyboard.spaceKey.isPressed)
                playerRigidbody.AddForce(
                    Physics.gravity * (shortJumpGravityMultiplier - 1f), ForceMode.Acceleration);
            else if (verticalSpeed < 0f)
                playerRigidbody.AddForce(
                    Physics.gravity * (fallGravityMultiplier - 1f), ForceMode.Acceleration);
        }
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (keyboard[flightToggleKey].wasPressedThisFrame)
            ToggleFlightMode();

        if (isFlightEnabled)
        {
            flightVerticalInput = 0f;
            if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
                flightVerticalInput += 1f;
            if (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)
                flightVerticalInput -= 1f;
            return;
        }
#endif

        UpdateMoveSpeed(keyboard);

        if (keyboard.spaceKey.wasPressedThisFrame)
            lastJumpPressedTime = Time.time;

        if (keyboard[attackKey].wasPressedThisFrame && isGrounded && !isAttacking)
            Attack();

        bool resetPressed = resetAction != null
            ? resetAction.WasPressedThisFrame()
            : keyboard[resetKey].wasPressedThisFrame;

        if (resetPressed && resetPoint != null)
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

        return (forward.normalized * vertical
              + right.normalized * horizontal).normalized;
    }

    void UpdateMoveSpeed(Keyboard keyboard)
    {
        bool isSpeedingUp = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        bool isSlowingDown = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;

        float targetSpeed = moveSpeed;
        if (isSpeedingUp && !isSlowingDown)
            targetSpeed = maximumMoveSpeed;
        else if (isSlowingDown && !isSpeedingUp)
            targetSpeed = minimumMoveSpeed;

        currentMoveSpeed = Mathf.MoveTowards(
            currentMoveSpeed,
            targetSpeed,
            speedChangeRate * Time.deltaTime);
    }

    void MovePlayer()
    {
        Vector3 velocity = playerRigidbody.linearVelocity;

        if (isAttacking)
        {
            // Keep the player stationary during an attack.
            velocity.x = 0f;
            velocity.z = 0f;
            playerRigidbody.linearVelocity = velocity;
            return;
        }

        if (!isGrounded)
        {
            // Steer from the current post-collision velocity to avoid repeatedly
            // restoring takeoff velocity and pushing the player into obstacles.
            if (moveDirection != Vector3.zero)
            {
                Vector3 targetVelocity = moveDirection * currentMoveSpeed;
                Vector3 horizontalVelocity = Vector3.MoveTowards(
                    new Vector3(velocity.x, 0f, velocity.z),
                    targetVelocity,
                    airControlAcceleration * Time.fixedDeltaTime);
                velocity.x = horizontalVelocity.x;
                velocity.z = horizontalVelocity.z;
            }
        }
        else
        {
            velocity.x = moveDirection.x * currentMoveSpeed;
            velocity.z = moveDirection.z * currentMoveSpeed;
        }

        playerRigidbody.linearVelocity = velocity;

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            Quaternion nextRotation = Quaternion.Slerp(
                playerRigidbody.rotation,
                targetRotation,
                turnSpeed * Time.fixedDeltaTime);
            playerRigidbody.MoveRotation(nextRotation);
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void MoveFlight()
    {
        isGrounded = false;

        Vector3 flightDirection = moveDirection + Vector3.up * flightVerticalInput;
        playerRigidbody.linearVelocity = flightDirection.normalized * flightSpeed;

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            playerRigidbody.MoveRotation(Quaternion.Slerp(
                playerRigidbody.rotation,
                targetRotation,
                turnSpeed * Time.fixedDeltaTime));
        }
    }

    void ToggleFlightMode()
    {
        isFlightEnabled = !isFlightEnabled;
        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;

        if (isFlightEnabled)
        {
            gravityWasEnabled = playerRigidbody.useGravity;
            playerRigidbody.useGravity = false;
            isGrounded = false;
            isAttacking = false;
            lastJumpPressedTime = float.NegativeInfinity;

            if (playerAnimator != null)
                playerAnimator.ResetTrigger("Attack");
        }
        else
        {
            playerRigidbody.useGravity = gravityWasEnabled;
        }

    }
#endif

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

        isGrounded = false;
        // Clear the grace period after takeoff to prevent unintended double jumps.
        lastGroundedTime = float.NegativeInfinity;

        if (playerAnimator != null)
            playerAnimator.SetTrigger("Jump");
    }

    bool CanJump()
    {
        return isGrounded || Time.time - lastGroundedTime <= groundedGraceTime;
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
        if (playerRigidbody == null)
            playerRigidbody = GetComponent<Rigidbody>();

        playerRigidbody.linearVelocity = bumpVelocity;
        isGrounded = false;
        lastGroundedTime = float.NegativeInfinity;
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
        StopAllCoroutines();
        playerRigidbody.position = resetPoint.position;
        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;

        moveDirection = Vector3.zero;
        isGrounded = false;
        isAttacking = false;
        lastGroundedTime = float.NegativeInfinity;
        lastJumpPressedTime = float.NegativeInfinity;
        enemyBumpEndTime = float.NegativeInfinity;
        freezeEndTime = float.NegativeInfinity;
        isFrozen = false;

        if (playerAnimator != null)
        {
            playerAnimator.speed = 1f;
            playerAnimator.ResetTrigger("Jump");
            playerAnimator.ResetTrigger("Attack");
            playerAnimator.Play("Base Layer.Idle", 0, 0f);
        }
    }

    void Attack()
    {
        if (playerAnimator == null)
            return;

        isAttacking = true;
        playerAnimator.SetTrigger("Attack");
        StartCoroutine(WaitForAttackToFinish());
    }

    IEnumerator WaitForAttackToFinish()
    {
        // The Animator processes the trigger after the current Update, so wait one frame.
        yield return null;

        // The attack transition may be cancelled if the grounded state changes after input.
        // Use a deadline so movement cannot remain locked indefinitely.
        float enterDeadline = Time.time + 0.5f;
        while (!playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Attack")
            && Time.time < enterDeadline)
            yield return null;

        if (!playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Attack"))
        {
            playerAnimator.ResetTrigger("Attack");
            isAttacking = false;
            yield break;
        }

        // Keep movement disabled until the attack state and exit blend are complete.
        float finishDeadline = Time.time + 2f;
        while (playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Attack")
            || playerAnimator.IsInTransition(0))
        {
            if (Time.time >= finishDeadline)
                break;
            yield return null;
        }

        isAttacking = false;
    }

    void UpdateGroundedState()
    {
        // Do not treat a platform near the feet as ground while the player is rising.
        if (playerRigidbody.linearVelocity.y > 0.1f || playerCollider == null)
        {
            isGrounded = false;
            return;
        }

        Bounds bounds = playerCollider.bounds;
        Vector3 origin = new Vector3(bounds.center.x, bounds.min.y + 0.35f, bounds.center.z);
        float sampleOffset = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.5f;
        // Thin beams can fall between ray samples, so use a sphere cast across the feet first.
        Vector3 sphereOrigin = new Vector3(bounds.center.x,
            bounds.min.y + 0.65f, bounds.center.z);
        float sphereRadius = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.7f;
        isGrounded = HasGroundBelowSphere(sphereOrigin, sphereRadius)
            || HasGroundBelow(origin)
            || HasGroundBelow(origin + Vector3.forward * sampleOffset)
            || HasGroundBelow(origin + Vector3.back * sampleOffset)
            || HasGroundBelow(origin + Vector3.left * sampleOffset)
            || HasGroundBelow(origin + Vector3.right * sampleOffset);

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
        }
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
            if (contact.normal.y >= minimumGroundNormalY)
            {
                isGrounded = true;
                lastGroundedTime = Time.time;

                ScoreObject scoreObject =
                    collision.collider.GetComponentInParent<ScoreObject>();

                if (scoreObject != null)
                    scoreObject.TryAwardPoint();

                return;
            }
        }
    }

    void UpdateAnimation()
    {
        if (playerAnimator == null)
            return;

        float animationMovement = isAttacking ? 0f : moveDirection.magnitude;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (isFlightEnabled)
            animationMovement = 0f;
#endif
        playerAnimator.SetFloat("Speed", animationMovement);
        // Reuse the running animation and scale its playback speed to actual movement speed.
        float runAnimationSpeed = currentMoveSpeed / Mathf.Max(moveSpeed, 0.01f);
        playerAnimator.SetFloat("AnimationSpeed", runAnimationSpeed);
        playerAnimator.SetBool("IsGrounded", isGrounded);
    }
}
