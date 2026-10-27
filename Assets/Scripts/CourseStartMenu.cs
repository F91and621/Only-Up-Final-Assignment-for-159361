using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
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

        if (playerController != null)
            playerController.enabled = false;

        if (cameraController != null)
        {
            cameraController.enabled = false;
            Camera camera = cameraController.GetComponent<Camera>();
            if (camera != null && playerController != null)
            {
                camera.transform.position = playerController.transform.position
                    + new Vector3(-10f, 8f, -12f);
                camera.transform.LookAt(playerController.transform.position
                    + new Vector3(4f, 4f, 5f));
            }
        }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        CreateMenu();
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
            Destroy(menuCanvas);
        if (playerController != null)
            playerController.enabled = true;
        if (cameraController != null)
            cameraController.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    void CreateMenu()
    {
        menuCanvas = new GameObject("Start Menu Canvas", typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = menuCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = menuCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform background = CreatePanel("Background", menuCanvas.transform,
            new Color(0f, 0f, 0f, 0.12f));
        Stretch(background);

        RectTransform panel = CreatePanel("Transparent Left Menu", background,
            new Color(0.04f, 0.07f, 0.11f, 0.22f));
        panel.anchorMin = new Vector2(0f, 0f);
        panel.anchorMax = new Vector2(0f, 1f);
        panel.pivot = new Vector2(0f, 0.5f);
        panel.sizeDelta = new Vector2(600f, 0f);
        panel.anchoredPosition = Vector2.zero;

        Text title = CreateText("Title", panel, font, "ONLY UP", 88,
            Color.white, Vector2.zero, new Vector2(480f, 118f));
        PlaceTopLeft(title.rectTransform, 92f, 100f);
        title.alignment = TextAnchor.MiddleLeft;
        Text subtitle = CreateText("Subtitle", panel, font, "FURNITURE CLIMB", 32,
            Color.white, Vector2.zero, new Vector2(470f, 54f));
        PlaceTopLeft(subtitle.rectTransform, 100f, 218f);
        subtitle.alignment = TextAnchor.MiddleLeft;

        RectTransform buttonRect = CreatePanel("Start Button", panel,
            new Color(1f, 1f, 1f, 0.96f));
        buttonRect.sizeDelta = new Vector2(360f, 78f);
        PlaceTopLeft(buttonRect, 96f, 520f);
        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonRect.GetComponent<Image>();
        button.targetGraphic.raycastTarget = true;
        button.onClick.AddListener(StartGame);
        Text buttonText = CreateText("Button Label", buttonRect, font, "START GAME", 33,
            new Color(0.06f, 0.08f, 0.1f, 1f), Vector2.zero,
            new Vector2(322f, 70f));
        buttonText.alignment = TextAnchor.MiddleLeft;

        CreateHintRow(panel, font, "WASD   MOVE", 620f);
        CreateHintRow(panel, font, "SPACE   JUMP", 688f);
        CreateHintRow(panel, font, "E   SAVE     R   RETURN", 756f);

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem",
                typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }

    RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        Image image = panel.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return panel.GetComponent<RectTransform>();
    }

    Text CreateText(string name, Transform parent, Font font, string value,
        int size, Color color, Vector2 position, Vector2 dimensions)
    {
        GameObject label = new GameObject(name, typeof(RectTransform), typeof(Text));
        label.transform.SetParent(parent, false);
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = dimensions;
        rect.anchoredPosition = position;

        Text text = label.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    void CreateHintRow(Transform parent, Font font, string value, float top)
    {
        RectTransform row = CreatePanel(value, parent,
            new Color(0.83f, 0.87f, 0.9f, 0.42f));
        row.sizeDelta = new Vector2(360f, 54f);
        PlaceTopLeft(row, 96f, top);
        Text label = CreateText("Hint", row, font, value, 23,
            new Color(0.08f, 0.1f, 0.13f, 1f), Vector2.zero,
            new Vector2(326f, 54f));
        label.alignment = TextAnchor.MiddleLeft;
    }

    void PlaceTopLeft(RectTransform rect, float x, float y)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
    }

    void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
