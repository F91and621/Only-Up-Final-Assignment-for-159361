using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 游戏开始前显示菜单，点击按钮后开启角色和镜头控制。
public class CourseStartMenu : MonoBehaviour
{
    private BeginnerPlayerController playerController;
    private BeginnerCameraController cameraController;
    private GameObject menuCanvas;
    private bool started;

    void Awake()
    {
        playerController = FindFirstObjectByType<BeginnerPlayerController>();
        cameraController = FindFirstObjectByType<BeginnerCameraController>();
        menuCanvas = GameObject.Find("Start Menu Canvas");

        if (menuCanvas == null)
        {
            StartGame();
            return;
        }

        SetControlsEnabled(false);
        SetPreviewCameraPosition();

        Button startButton = menuCanvas.GetComponentInChildren<Button>();
        if (startButton != null)
            startButton.onClick.AddListener(StartGame);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (!started && keyboard != null && keyboard.enterKey.wasPressedThisFrame)
            StartGame();
    }

    public void StartGame()
    {
        if (started)
            return;

        started = true;
        Time.timeScale = 1f;

        if (menuCanvas != null)
            menuCanvas.SetActive(false);

        SetControlsEnabled(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    void SetControlsEnabled(bool enabled)
    {
        if (playerController != null)
            playerController.enabled = enabled;

        if (cameraController != null)
            cameraController.enabled = enabled;
    }

    void SetPreviewCameraPosition()
    {
        if (cameraController == null || playerController == null)
            return;

        Camera camera = cameraController.GetComponent<Camera>();
        if (camera == null)
            return;

        camera.transform.position = playerController.transform.position
            + new Vector3(-10f, 8f, -12f);
        camera.transform.LookAt(playerController.transform.position
            + new Vector3(4f, 4f, 5f));
    }
}
