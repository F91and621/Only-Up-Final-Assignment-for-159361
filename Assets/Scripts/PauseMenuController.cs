using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Pause menu control.
public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject pauseCard;
    [SerializeField] private GameObject settingsCard;
    [SerializeField] private Text volumeText;

    private bool isPaused;
    private const string VolumeKey = "GameVolume";

    void Start()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 1f);

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
        {
            PauseGame();
        }
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
        SceneManager.LoadScene(gameObject.scene.path);
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
}
