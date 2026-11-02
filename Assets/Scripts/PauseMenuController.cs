using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// 按 Esc 暂停游戏，也负责重新加载当前场景。
public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject loadingPanel;

    private bool isPaused;

    void Start()
    {
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
            ResumeGame();
        else if (Time.timeScale > 0f)
            PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;
        if (pausePanel != null)
            pausePanel.SetActive(true);

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
}
