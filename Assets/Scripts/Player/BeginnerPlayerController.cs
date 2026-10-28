using UnityEngine;
using UnityEngine.InputSystem;

// 读取按键，并管理角色的重置和冻结。
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BeginnerPlayerMovement))]
[RequireComponent(typeof(BeginnerPlayerAnimation))]
public class BeginnerPlayerController : MonoBehaviour
{
    [Header("Reset")]
    public Key resetKey = Key.R;
    public Transform resetPoint;

    private Rigidbody playerRigidbody;
    private BeginnerPlayerMovement playerMovement;
    private BeginnerPlayerAnimation playerAnimation;

    private float horizontalInput;
    private float verticalInput;
    private bool jumpRequested;
    private bool resetRequested;
    private float freezeTimer;
    private RigidbodyConstraints normalConstraints;

    void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        playerMovement = GetComponent<BeginnerPlayerMovement>();
        playerAnimation = GetComponent<BeginnerPlayerAnimation>();

        playerRigidbody.constraints |= RigidbodyConstraints.FreezeRotation;
        normalConstraints = playerRigidbody.constraints;
    }

    void Start()
    {
        FindResetPoint();
    }

    void Update()
    {
        UpdateFreezeTimer();
        ReadInput();

        playerAnimation.UpdateAnimation(
            playerMovement.MoveDirection,
            playerMovement.IsGrounded);
    }

    void FixedUpdate()
    {
        if (resetRequested)
        {
            ResetPlayer();
            resetRequested = false;
            return;
        }

        if (freezeTimer > 0f)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerMovement.StopMovement();
            jumpRequested = false;
            return;
        }

        bool jumped = playerMovement.Move(
            horizontalInput, verticalInput, jumpRequested);

        if (jumped)
            playerAnimation.PlayJump();

        jumpRequested = false;
    }

    void ReadInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        horizontalInput = 0f;
        verticalInput = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            horizontalInput = -1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            horizontalInput = 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            verticalInput = -1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            verticalInput = 1f;

        if (keyboard.spaceKey.wasPressedThisFrame)
            jumpRequested = true;

        if (keyboard[resetKey].wasPressedThisFrame && resetPoint != null)
            resetRequested = true;
    }

    void FindResetPoint()
    {
        if (resetPoint != null)
            return;

        GameObject point = GameObject.Find("ResetPoint");
        if (point == null)
            point = GameObject.Find("resetpoint");

        if (point != null)
            resetPoint = point.transform;
    }

    public void SetResetPoint(Transform point)
    {
        if (point != null)
            resetPoint = point;
    }

    public void FreezeForSeconds(float seconds)
    {
        freezeTimer = seconds;
        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;
        playerRigidbody.constraints = RigidbodyConstraints.FreezeAll;
        playerAnimation.SetFrozen(true);
    }

    void UpdateFreezeTimer()
    {
        if (freezeTimer <= 0f)
            return;

        freezeTimer -= Time.deltaTime;
        if (freezeTimer > 0f)
            return;

        freezeTimer = 0f;
        playerRigidbody.constraints = normalConstraints;
        playerAnimation.SetFrozen(false);
    }

    void ResetPlayer()
    {
        playerRigidbody.position = resetPoint.position;
        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;
        playerRigidbody.constraints = normalConstraints;

        horizontalInput = 0f;
        verticalInput = 0f;
        jumpRequested = false;
        freezeTimer = 0f;

        playerMovement.ResetMovement();
        playerAnimation.ResetAnimation();
    }
}
