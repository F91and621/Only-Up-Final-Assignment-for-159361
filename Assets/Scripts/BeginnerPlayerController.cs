using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// 控制玩家移动、跳跃和动画。
[RequireComponent(typeof(Rigidbody))]
public class BeginnerPlayerController : MonoBehaviour
{
    [Header("移动")]
    public float moveSpeed = 5f;
    public float turnSpeed = 10f;

    [Header("速度控制")]
    public float minimumMoveSpeed = 2f;
    public float maximumMoveSpeed = 8f;
    public float speedChangeRate = 12f;

    [Header("跳跃")]
    public float jumpSpeed = 7f;
    [Tooltip("松开空格后上升阶段的重力倍率；轻按短跳，按住保持原有高度。")]
    public float shortJumpGravityMultiplier = 2.2f;
    [Tooltip("下落时的重力倍率；不改变起跳高度，只缩短落地等待。")]
    public float fallGravityMultiplier = 1.8f;
    [Tooltip("落地前按下跳跃键时，保留输入的时间。")]
    public float jumpBufferTime = 0.15f;
    public float minimumGroundNormalY = 0.4f;
    [Tooltip("空中改变水平方向的速度，避免跳到障碍边缘后一直被卡住。")]
    public float airControlAcceleration = 16f;
    [Tooltip("离开不平整地面后，仍可起跳的短暂时间。")]
    public float groundedGraceTime = 0.15f;

    [Header("攻击")]
    public Key attackKey = Key.F;

    [Header("重置")]
    public Transform resetPoint;
    [Tooltip("包含 Player/Reset Action 的 Input Actions 资产。")]
    public InputActionAsset playerActions;
    public Key resetKey = Key.R;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [Header("测试飞行（仅 Editor / Development Build）")]
    [Tooltip("按此键切换测试飞行模式。")]
    public Key flightToggleKey = Key.F2;
    [Tooltip("飞行时的移动速度。Shift 上升，Ctrl 下降，WASD 水平移动。")]
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private bool isFlightEnabled;
    private bool gravityWasEnabled;
    private float flightVerticalInput;
#endif

    void Start()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
        // 碰撞会对刚体施加扭矩；锁住物理旋转，避免撞墙时改变角色朝向。
        // 角色转向仍由 MoveRotation 在 MovePlayer 中明确控制。
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
            // 攻击期间原地站定，避免输入或惯性让角色继续移动。
            velocity.x = 0f;
            velocity.z = 0f;
            playerRigidbody.linearVelocity = velocity;
            return;
        }

        if (!isGrounded)
        {
            // 以碰撞后的真实速度为起点转向，避免反复把起跳速度写回、把角色推在障碍上。
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

        Debug.Log($"Test flight mode: {(isFlightEnabled ? "enabled" : "disabled")}");
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
        // 起跳后清除宽限时间，防止连续按键造成二段跳。
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

        if (playerAnimator != null)
        {
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
        // Animator 会在当前 Update 结束后处理 Trigger，因此先等待一帧。
        yield return null;

        // 地面状态在按键后改变时，攻击转场可能被取消；不能无限锁住移动。
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

        // 攻击状态及其退出混合完成前，保持禁止移动。
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
        // 上升阶段不能把仍贴近脚底的平台重新判定为落地。
        if (playerRigidbody.linearVelocity.y > 0.1f || playerCollider == null)
        {
            isGrounded = false;
            return;
        }

        Bounds bounds = playerCollider.bounds;
        Vector3 origin = new Vector3(bounds.center.x, bounds.min.y + 0.35f, bounds.center.z);
        float sampleOffset = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.5f;
        // 细木梁可能恰好落在射线之间；先用球体覆盖脚掌范围。
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
        // 复用跑步动画，并按实际移动速度调整其播放速度。
        float runAnimationSpeed = currentMoveSpeed / Mathf.Max(moveSpeed, 0.01f);
        playerAnimator.SetFloat("AnimationSpeed", runAnimationSpeed);
        playerAnimator.SetBool("IsGrounded", isGrounded);
    }
}
