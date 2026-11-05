using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Creates the start menu, help page, and records page.
public class CourseStartMenu : MonoBehaviour
{
    private BeginnerPlayerController playerController;
    private BeginnerCameraController cameraController;
    private GameObject menuCanvas;
    private RectTransform pagePanel;
    private Font font;
    private Text volumeText;
    private bool started;
    private bool finished;
    private int currentPage;

    private const string VolumeKey = "GameVolume";

    void Awake()
    {
        playerController = FindFirstObjectByType<BeginnerPlayerController>();
        cameraController = FindFirstObjectByType<BeginnerCameraController>();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        StartBackgroundMusic();

        SetPlayerControl(false);
        Time.timeScale = 0f;
        UnlockMouse();

        CreateMenu();
        ShowMainPage();
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if ((!started || finished) && currentPage != 0 && keyboard != null
            && keyboard.escapeKey.wasPressedThisFrame)
        {
            ShowMainPage();
            return;
        }

        if (!started && currentPage == 0 && keyboard != null
            && keyboard.enterKey.wasPressedThisFrame)
        {
            StartGame();
        }
    }

    public void StartGame()
    {
        if (finished)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameObject.scene.path);
            return;
        }

        if (started)
            return;

        started = true;
        Time.timeScale = 1f;
        SetPlayerControl(true);

        if (menuCanvas != null)
        {
            Destroy(menuCanvas);
            menuCanvas = null;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void FinishGame()
    {
        if (finished)
            return;

        finished = true;
        float finalTime = 0f;
        if (PlayerProgressUI.Instance != null)
            finalTime = PlayerProgressUI.Instance.GetGameTime();

        RunRecordManager.AddRecord(finalTime);
        SetPlayerControl(false);
        Time.timeScale = 0f;
        UnlockMouse();

        if (menuCanvas == null)
            CreateMenu();

        ShowRecordsPage();
    }

    public void ShowMainPage()
    {
        currentPage = 0;
        ClearPage();

        pagePanel = CreatePanel("Main Buttons", menuCanvas.transform,
            Color.clear);
        SetRect(pagePanel, new Vector2(470f, 390f), new Vector2(-610f, -185f));
        CreateButton(pagePanel, "START GAME", 45f, StartGame);
        CreateButton(pagePanel, "HOW TO PLAY", -50f, ShowHelpPage);
        CreateButton(pagePanel, "RECORDS", -145f, ShowRecordsPage);
        CreateButton(pagePanel, "SETTINGS", -240f, ShowSettingsPage);
    }

    public void ShowHelpPage()
    {
        currentPage = 1;
        ClearPage();

        pagePanel = CreatePanel("How To Play Page", menuCanvas.transform, Color.white);
        SetRect(pagePanel, new Vector2(980f, 790f), Vector2.zero);
        AddBlackBorder(pagePanel.gameObject);

        CreateText("Help Title", pagePanel, "HOW TO PLAY", 50,
            new Vector2(0f, 315f), new Vector2(860f, 75f));

        string help =
            "WASD  -  MOVE\n" +
            "SPACE  -  JUMP\n" +
            "E  -  SAVE CHECKPOINT\n" +
            "R  -  RETURN TO CHECKPOINT\n" +
            "ESC  -  PAUSE\n\n" +
            "ENEMIES\n" +
            "SLIME - Patrols platforms. Stomp its head to defeat it and bounce.\n" +
            "Touching its side knocks you back.\n" +
            "BIRD - Flies up and down. Time your jump and stomp its head\n" +
            "for a stronger bounce. Side contact knocks you back.\n" +
            "GUARD - Patrols and chases you when you get close.\n" +
            "It cannot be stomped; touching it knocks you back.\n\n" +
            "TOWER\n" +
            "Inside 50 metres, the tower attacks every 10 seconds.\n" +
            "Keep moving and dodge the blue ball. A hit freezes you for 1 second.\n" +
            "Climb out of the tower's range to continue safely.";

        Text helpText = CreateText("Help Text", pagePanel, help, 24,
            new Vector2(0f, 25f), new Vector2(850f, 500f));
        helpText.alignment = TextAnchor.UpperLeft;
        helpText.lineSpacing = 1.05f;

        CreateButton(pagePanel, "BACK", -320f, ShowMainPage);
    }

    public void ShowRecordsPage()
    {
        currentPage = 2;
        ClearPage();

        pagePanel = CreatePanel("Records Page", menuCanvas.transform, Color.white);
        SetRect(pagePanel, new Vector2(980f, 790f), Vector2.zero);
        AddBlackBorder(pagePanel.gameObject);

        CreateText("Records Title", pagePanel, "RECORDS", 50,
            new Vector2(0f, 320f), new Vector2(860f, 70f));

        Text recordsText = CreateText("Records List", pagePanel,
            BuildRecordsText(), 27, new Vector2(0f, 25f), new Vector2(860f, 500f));
        recordsText.alignment = TextAnchor.UpperLeft;
        recordsText.lineSpacing = 1.25f;

        CreateButton(pagePanel, "BACK", -320f, ShowMainPage);
    }

    public void ShowSettingsPage()
    {
        currentPage = 3;
        ClearPage();

        pagePanel = CreatePanel("Settings Page", menuCanvas.transform, Color.white);
        SetRect(pagePanel, new Vector2(700f, 600f), Vector2.zero);
        AddBlackBorder(pagePanel.gameObject);

        CreateText("Settings Title", pagePanel, "SETTINGS", 50,
            new Vector2(0f, 225f), new Vector2(600f, 70f));

        volumeText = CreateText("Volume Text", pagePanel, "", 34,
            new Vector2(0f, 125f), new Vector2(500f, 60f));
        UpdateVolumeText();

        CreateButton(pagePanel, "VOLUME +", 25f, IncreaseVolume);
        CreateButton(pagePanel, "VOLUME -", -70f, DecreaseVolume);
        CreateButton(pagePanel, "BACK", -220f, ShowMainPage);
    }

    void IncreaseVolume()
    {
        ChangeVolume(0.1f);
    }

    void DecreaseVolume()
    {
        ChangeVolume(-0.1f);
    }

    void ChangeVolume(float amount)
    {
        AudioListener.volume = Mathf.Clamp01(AudioListener.volume + amount);
        PlayerPrefs.SetFloat(VolumeKey, AudioListener.volume);
        PlayerPrefs.Save();
        UpdateVolumeText();
    }

    void UpdateVolumeText()
    {
        if (volumeText != null)
            volumeText.text = "VOLUME: "
                + Mathf.RoundToInt(AudioListener.volume * 100f) + "%";
    }

    void StartBackgroundMusic()
    {
        AudioClip music = Resources.Load<AudioClip>("Audio/BackgroundMusic");
        if (music == null)
            return;

        AudioSource source = gameObject.GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        source.clip = music;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.Play();
    }

    string BuildRecordsText()
    {
        var records = RunRecordManager.GetSortedRecords();
        if (records.Count == 0)
            return "NO COMPLETED RUNS YET";

        string result = "RANK     DATE / TIME              DURATION\n\n";
        for (int i = 0; i < records.Count; i++)
        {
            RunRecord record = records[i];
            string current = record.id == RunRecordManager.CurrentId
                ? "   CURRENT" : "";

            result += (i + 1) + ".     " + record.date + "        "
                + RunRecordManager.FormatTime(record.duration) + current + "\n";
        }

        return result;
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

        GameObject cover = new GameObject("Game Cover", typeof(RectTransform),
            typeof(RawImage));
        cover.transform.SetParent(menuCanvas.transform, false);
        RawImage coverImage = cover.GetComponent<RawImage>();
        coverImage.texture = Resources.Load<Texture2D>("UI/OnlyUpCover");
        coverImage.color = Color.white;
        coverImage.raycastTarget = false;
        Stretch(cover.GetComponent<RectTransform>());

        RectTransform shade = CreatePanel("Cover Shade", menuCanvas.transform,
            new Color(0f, 0f, 0f, 0.16f));
        Stretch(shade);

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem",
                typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }

    Button CreateButton(Transform parent, string label, float y,
        UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = CreatePanel(label + " Button", parent, Color.white);
        SetRect(rect, new Vector2(350f, 72f), new Vector2(0f, y));
        AddBlackBorder(rect.gameObject);

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        button.targetGraphic.raycastTarget = true;
        button.onClick.AddListener(action);

        CreateText("Label", rect, label, 30, Vector2.zero,
            new Vector2(330f, 66f));
        return button;
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

    Text CreateText(string name, Transform parent, string value, int size,
        Vector2 position, Vector2 dimensions)
    {
        GameObject label = new GameObject(name, typeof(RectTransform), typeof(Text));
        label.transform.SetParent(parent, false);

        RectTransform rect = label.GetComponent<RectTransform>();
        SetRect(rect, dimensions, position);

        Text text = label.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;
        text.raycastTarget = false;
        return text;
    }

    void SetPlayerControl(bool enabled)
    {
        if (playerController != null)
            playerController.enabled = enabled;
        if (cameraController != null)
            cameraController.enabled = enabled;
    }

    void UnlockMouse()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ClearPage()
    {
        if (pagePanel != null)
            Destroy(pagePanel.gameObject);
        pagePanel = null;
        volumeText = null;
    }

    void SetRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    void AddBlackBorder(GameObject target)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(4f, -4f);
        outline.useGraphicAlpha = true;
    }

    void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
