using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Pause and restart.
public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject loadingPanel;

    private bool isPaused;
    private GameObject pauseCard;
    private GameObject settingsCard;
    private Text volumeText;
    private Font font;

    private const string VolumeKey = "GameVolume";

    void Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        SetupPauseMenu();

        if (pausePanel != null)
            pausePanel.SetActive(false);
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
            return;

        if (isPaused)
        {
            if (settingsCard != null && settingsCard.activeSelf)
                ShowPauseMenu();
            else
                ResumeGame();
        }
        else if (Time.timeScale > 0f)
            PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;
        if (pausePanel != null)
            pausePanel.SetActive(true);
        ShowPauseMenu();

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pausePanel != null)
            pausePanel.SetActive(false);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ShowSettings()
    {
        if (pauseCard != null)
            pauseCard.SetActive(false);
        if (settingsCard != null)
            settingsCard.SetActive(true);

        UpdateVolumeText();
    }

    public void ShowPauseMenu()
    {
        if (settingsCard != null)
            settingsCard.SetActive(false);
        if (pauseCard != null)
            pauseCard.SetActive(true);
    }

    public void RestartGame()
    {
        StartCoroutine(RestartRoutine());
    }

    IEnumerator RestartRoutine()
    {
        isPaused = false;
        if (pausePanel != null)
            pausePanel.SetActive(false);
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        Time.timeScale = 1f;
        yield return null;

        string scenePath = gameObject.scene.path;
        SceneManager.LoadScene(scenePath);
    }

    void SetupPauseMenu()
    {
        if (pausePanel == null)
            return;

        Transform card = pausePanel.transform.Find("Pause Card");
        if (card == null)
            return;

        pauseCard = card.gameObject;
        SetY(card.Find("Pause Title"), 150f);
        SetY(card.Find("Resume Button"), 55f);
        SetY(card.Find("Pause Restart Button"), -135f);

        CreateButton(card, "Settings Button", "SETTINGS", -40f, ShowSettings);

        RectTransform settings = CreateWhiteBox("Settings Card",
            pausePanel.transform, new Vector2(560f, 440f), Vector2.zero);
        settingsCard = settings.gameObject;

        CreateText(settings, "Settings Title", "SETTINGS", 50,
            new Vector2(0f, 150f), new Vector2(500f, 70f));
        volumeText = CreateText(settings, "Volume Text", "", 32,
            new Vector2(0f, 75f), new Vector2(450f, 55f));

        CreateButton(settings, "Volume Up Button", "VOLUME +", 5f,
            IncreaseVolume);
        CreateButton(settings, "Volume Down Button", "VOLUME -", -85f,
            DecreaseVolume);
        CreateButton(settings, "Back Button", "BACK", -175f,
            ShowPauseMenu);

        settingsCard.SetActive(false);
        UpdateVolumeText();
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

    void SetY(Transform item, float y)
    {
        if (item == null)
            return;

        RectTransform rect = item.GetComponent<RectTransform>();
        if (rect != null)
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
    }

    void CreateButton(Transform parent, string name, string label,
        float y, UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = CreateWhiteBox(name, parent,
            new Vector2(340f, 76f), new Vector2(0f, y));

        Image image = rect.GetComponent<Image>();
        image.raycastTarget = true;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        CreateText(rect, "Label", label, 30, Vector2.zero,
            new Vector2(320f, 66f));
    }

    RectTransform CreateWhiteBox(string name, Transform parent,
        Vector2 size, Vector2 position)
    {
        GameObject box = new GameObject(name, typeof(RectTransform),
            typeof(Image), typeof(Outline));
        box.transform.SetParent(parent, false);

        RectTransform rect = box.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = box.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;

        Outline outline = box.GetComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3f, -3f);
        return rect;
    }

    Text CreateText(Transform parent, string name, string value, int size,
        Vector2 position, Vector2 dimensions)
    {
        GameObject label = new GameObject(name, typeof(RectTransform), typeof(Text));
        label.transform.SetParent(parent, false);

        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = dimensions;
        rect.anchoredPosition = position;

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
}
