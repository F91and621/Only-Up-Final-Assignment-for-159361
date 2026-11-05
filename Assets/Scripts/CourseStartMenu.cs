using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Start menu control.
public class CourseStartMenu : MonoBehaviour
{
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private GameObject mainPage;
    [SerializeField] private GameObject helpPage;
    [SerializeField] private GameObject recordsPage;
    [SerializeField] private GameObject settingsPage;
    [SerializeField] private Text recordsText;
    [SerializeField] private Text volumeText;

    private BeginnerPlayerController playerController;
    private BeginnerCameraController cameraController;
    private bool started;
    private bool finished;
    private int currentPage;

    private const string VolumeKey = "GameVolume";

    void Awake()
    {
        playerController = FindFirstObjectByType<BeginnerPlayerController>();
        cameraController = FindFirstObjectByType<BeginnerCameraController>();

        AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        StartBackgroundMusic();
        SetPlayerControl(false);
        Time.timeScale = 0f;
        UnlockMouse();
        ShowMainPage();
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if ((!started || finished) && currentPage != 0
            && keyboard.escapeKey.wasPressedThisFrame)
        {
            ShowMainPage();
        }
        else if (!started && currentPage == 0
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
            menuCanvas.SetActive(false);

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

        if (menuCanvas != null)
            menuCanvas.SetActive(true);

        ShowRecordsPage();
    }

    public void ShowMainPage()
    {
        currentPage = 0;
        ShowPage(mainPage);
    }

    public void ShowHelpPage()
    {
        currentPage = 1;
        ShowPage(helpPage);
    }

    public void ShowRecordsPage()
    {
        currentPage = 2;
        ShowPage(recordsPage);

        if (recordsText != null)
            recordsText.text = BuildRecordsText();
    }

    public void ShowSettingsPage()
    {
        currentPage = 3;
        ShowPage(settingsPage);
        UpdateVolumeText();
    }

    public void IncreaseVolume()
    {
        ChangeVolume(0.1f);
    }

    public void DecreaseVolume()
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
        {
            int percent = Mathf.RoundToInt(AudioListener.volume * 100f);
            volumeText.text = "VOLUME: " + percent + "%";
        }
    }

    void StartBackgroundMusic()
    {
        AudioClip music = Resources.Load<AudioClip>("Audio/BackgroundMusic");
        if (music == null)
            return;

        AudioSource source = GetComponent<AudioSource>();
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

    void ShowPage(GameObject page)
    {
        if (mainPage != null)
            mainPage.SetActive(page == mainPage);
        if (helpPage != null)
            helpPage.SetActive(page == helpPage);
        if (recordsPage != null)
            recordsPage.SetActive(page == recordsPage);
        if (settingsPage != null)
            settingsPage.SetActive(page == settingsPage);
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

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
