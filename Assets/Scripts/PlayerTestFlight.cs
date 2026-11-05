using UnityEngine;
using UnityEngine.InputSystem;

// Editor flight test.
public class PlayerTestFlight : MonoBehaviour
{
    public Key flightToggleKey = Key.F2;
    public float flightSpeed = 12f;
    public float turnSpeed = 10f;

    private BeginnerPlayerController playerController;
    private PlayerAttackController attackController;
    private Rigidbody playerRigidbody;
    private Animator playerAnimator;
    private Camera mainCamera;
    private Vector3 flightDirection;
    private bool flightEnabled;
    private bool gravityWasEnabled;

    void Awake()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        playerController = GetComponent<BeginnerPlayerController>();
        attackController = GetComponent<PlayerAttackController>();
        playerRigidbody = GetComponent<Rigidbody>();
        playerAnimator = GetComponentInChildren<Animator>();
        mainCamera = Camera.main;
#else
        enabled = false;
#endif
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (!flightEnabled && (playerController == null || !playerController.enabled))
            return;

        if (keyboard[flightToggleKey].wasPressedThisFrame)
            ToggleFlight();
        if (flightEnabled)
            flightDirection = ReadFlightDirection(keyboard);
    }

    void FixedUpdate()
    {
        if (!flightEnabled)
            return;

        playerRigidbody.linearVelocity = flightDirection.normalized * flightSpeed;
        Vector3 horizontal = new Vector3(flightDirection.x, 0f, flightDirection.z);
        if (horizontal != Vector3.zero)
        {
            Quaternion target = Quaternion.LookRotation(horizontal);
            playerRigidbody.MoveRotation(Quaternion.Slerp(
                playerRigidbody.rotation, target, turnSpeed * Time.fixedDeltaTime));
        }
    }

    Vector3 ReadFlightDirection(Keyboard keyboard)
    {
        float horizontal = 0f;
        float vertical = 0f;
        float height = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            horizontal = -1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            horizontal = 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            vertical = -1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            vertical = 1f;
        if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
            height = 1f;
        if (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)
            height = -1f;

        if (mainCamera == null)
            return Vector3.up * height;
        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;
        forward.y = 0f;
        right.y = 0f;
        return forward.normalized * vertical + right.normalized * horizontal
            + Vector3.up * height;
    }

    void ToggleFlight()
    {
        flightEnabled = !flightEnabled;
        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;

        if (flightEnabled)
        {
            gravityWasEnabled = playerRigidbody.useGravity;
            playerRigidbody.useGravity = false;
            if (attackController != null)
                attackController.CancelAttack();
            playerController.enabled = false;
            if (playerAnimator != null)
            {
                playerAnimator.SetFloat("Speed", 0f);
                playerAnimator.SetBool("IsGrounded", false);
            }
        }
        else
        {
            playerRigidbody.useGravity = gravityWasEnabled;
            playerController.enabled = true;
        }
    }

    void OnValidate()
    {
        flightSpeed = Mathf.Max(0f, flightSpeed);
    }
}
