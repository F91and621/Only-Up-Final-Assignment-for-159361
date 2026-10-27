using UnityEngine;
using UnityEngine.InputSystem;

// 让摄像机跟随玩家，并使用鼠标旋转视角�?
[RequireComponent(typeof(Camera))]
public class BeginnerCameraController : MonoBehaviour
{
    public float distance = 6f;
    public float targetHeight = 1.5f;
    public float mouseSensitivity = 0.12f;
    public float zoomSensitivity = 0.3f;
    public float minimumDistance = 3f;
    public float maximumDistance = 10f;
    public float minPitch = -20f;
    public float maxPitch = 60f;

    private Transform playerTransform;
    private float yaw;
    private float pitch = 15f;

    void Start()
    {
        transform.SetParent(null, true);

        BeginnerPlayerController player =
            FindFirstObjectByType<BeginnerPlayerController>();

        if (player != null)
        {
            playerTransform = player.transform;
            yaw = playerTransform.eulerAngles.y;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;

        if (mouse != null)
        {
            // 滚轮向上时拉近镜头，向下时拉远镜头�?
            float scroll = mouse.scroll.ReadValue().y;
            distance -= scroll * zoomSensitivity;
            distance = Mathf.Clamp(distance, minimumDistance, maximumDistance);

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 mouseMove = mouse.delta.ReadValue();
                yaw += mouseMove.x * mouseSensitivity;
                pitch -= mouseMove.y * mouseSensitivity;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }
        }

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void LateUpdate()
    {
        if (playerTransform == null)
            return;

        Vector3 targetPosition =
            playerTransform.position + Vector3.up * targetHeight;
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -distance);

        transform.position = targetPosition + offset;
        transform.LookAt(targetPosition);
    }

    void OnValidate()
    {
        minimumDistance = Mathf.Max(0.1f, minimumDistance);
        maximumDistance = Mathf.Max(minimumDistance, maximumDistance);
        distance = Mathf.Clamp(distance, minimumDistance, maximumDistance);
        zoomSensitivity = Mathf.Max(0f, zoomSensitivity);
    }
}
