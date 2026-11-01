using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverController : MonoBehaviour
{
    [Header("Game Over Interface")]
    [SerializeField] private GameObject gameOverPanel;

    public void ShowGameOver(BeginnerPlayerController player)
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (player != null)
        {
            player.enabled = false;

            Rigidbody playerBody = player.GetComponent<Rigidbody>();

            if (playerBody != null)
            {
                playerBody.linearVelocity = Vector3.zero;
                playerBody.angularVelocity = Vector3.zero;
            }
        }

        BeginnerCameraController cameraController =
            FindFirstObjectByType<BeginnerCameraController>();

        if (cameraController != null)
            cameraController.enabled = false;

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }
}