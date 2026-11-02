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
        canvas.pixelPerfect = true;

        CanvasScaler scaler = menuCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform background = CreatePanel("Background", menuCanvas.transform,
            new Color(0f, 0f, 0f, 0.45f));
        Stretch(background);

        RectTransform panel = CreatePanel("Center Menu", background, Color.white);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(680f, 820f);
        panel.anchoredPosition = Vector2.zero;
        AddBlackBorder(panel.gameObject);

        CreateText("Title", panel, font, "ONLY UP", 88,
            Color.black, new Vector2(0f, 285f), new Vector2(560f, 118f));
        CreateText("Subtitle", panel, font, "FURNITURE CLIMB", 32,
            Color.black, new Vector2(0f, 210f), new Vector2(520f, 54f));

        RectTransform buttonRect = CreatePanel("Start Button", panel, Color.white);
        buttonRect.sizeDelta = new Vector2(360f, 78f);
        buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, 90f);
        AddBlackBorder(buttonRect.gameObject);
        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonRect.GetComponent<Image>();
        button.targetGraphic.raycastTarget = true;
        button.onClick.AddListener(StartGame);
        CreateText("Button Label", buttonRect, font, "START GAME", 33,
            Color.black, Vector2.zero,
            new Vector2(322f, 70f));

        CreateHintRow(panel, font, "WASD   MOVE", -20f);
        CreateHintRow(panel, font, "SPACE   JUMP", -90f);
        CreateHintRow(panel, font, "E   SAVE     R   RETURN", -160f);
        CreateHintRow(panel, font, "ESC   PAUSE", -230f);

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
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    void CreateHintRow(Transform parent, Font font, string value, float y)
    {
        RectTransform row = CreatePanel(value, parent, Color.white);
        row.sizeDelta = new Vector2(360f, 54f);
        row.anchorMin = row.anchorMax = new Vector2(0.5f, 0.5f);
        row.anchoredPosition = new Vector2(0f, y);
        AddBlackBorder(row.gameObject);
        CreateText("Hint", row, font, value, 23,
            Color.black, Vector2.zero,
            new Vector2(326f, 54f));
    }

    void AddBlackBorder(GameObject target)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3f, -3f);
        outline.useGraphicAlpha = true;
    }

    void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
