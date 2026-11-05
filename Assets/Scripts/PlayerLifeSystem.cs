using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerLifeSystem : MonoBehaviour
{
    [SerializeField] private int startingLives = 3;
    [FormerlySerializedAs("fallDistanceBelowCheckpoint")]
    [SerializeField] private float fallDistance = 5f;
    [SerializeField] private Text lifeText;
    [SerializeField] private GameOverController gameOverController;

    private int currentLives;
    private bool livesActive;
    private Transform respawnPoint;
    private BeginnerPlayerController player;
    private Rigidbody playerBody;
    private float fallStartY;
    private bool fallHandled;

    bool IsGameOver => currentLives <= 0;

    void Awake()
    {
        currentLives = Mathf.Max(1, startingLives);
        if (gameOverController == null)
            gameOverController = GetComponent<GameOverController>();
        UpdateLifeText();
    }

    void Start()
    {
        player = FindFirstObjectByType<BeginnerPlayerController>();
        if (player == null || player.resetPoint == null)
            return;

        playerBody = player.GetComponent<Rigidbody>();
        respawnPoint = player.resetPoint;
        fallStartY = player.transform.position.y;
        livesActive = true;
        UpdateLifeText();
    }

    void Update()
    {
        if (!livesActive || IsGameOver || player == null
            || playerBody == null || respawnPoint == null)
            return;

        float playerY = player.transform.position.y;
        if (fallHandled)
        {
            Vector3 offset = player.transform.position - respawnPoint.position;
            if (offset.sqrMagnitude <= 4f)
            {
                fallHandled = false;
                fallStartY = playerY;
            }
            return;
        }

        if (playerBody.linearVelocity.y >= -0.1f)
        {
            fallStartY = playerY;
            return;
        }
        if (fallStartY - playerY >= fallDistance)
            HandleFall();
    }

    public void ActivateLives(Transform checkpoint,
        BeginnerPlayerController playerController)
    {
        if (checkpoint == null || playerController == null)
            return;

        respawnPoint = checkpoint;
        player = playerController;
        playerBody = player.GetComponent<Rigidbody>();
        fallStartY = player.transform.position.y;
        livesActive = true;
        fallHandled = false;
        UpdateLifeText();
    }

    void HandleFall()
    {
        fallHandled = true;
        currentLives--;
        UpdateLifeText();

        if (currentLives > 0)
            player.RespawnAtResetPoint();
        else if (gameOverController != null)
            gameOverController.ShowGameOver(player);
    }

    void UpdateLifeText()
    {
        if (lifeText != null)
            lifeText.text = $"LIVES: {currentLives}";
    }
}
