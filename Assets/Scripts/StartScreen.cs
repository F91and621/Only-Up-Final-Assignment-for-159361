using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class StartScreen : MonoBehaviour
{
    [Header("相机")]
    public CinemachineCamera startCam;    // 开始界面机位
    public CinemachineCamera playerCam;   // 角色跟随机位(PlayerFollowCamera)

    [Header("可选")]
    public GameObject startUI;            // 开始界面 UI,点击后隐藏
    public MonoBehaviour[] playerControls; // 开始前要禁用的角色控制脚本
    public float enableDelay = 2f;        // 等镜头过渡结束再允许操作,和 Brain 的 Default Blend 时长一致

    private bool started = false;

    void Start()
    {
        startCam.Priority = 20;
        playerCam.Priority = 10;

        foreach (var c in playerControls)
            if (c != null) c.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (started) return;  // 已开始,再点左键无效

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            started = true;

            startCam.Priority = 5;
            playerCam.Priority = 20;

            if (startUI != null) startUI.SetActive(false);

            Invoke(nameof(EnableControl), enableDelay);
        }
    }

    void EnableControl()
    {
        foreach (var c in playerControls)
            if (c != null) c.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}