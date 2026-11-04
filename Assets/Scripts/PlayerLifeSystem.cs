using UnityEngine;
using UnityEngine.Serialization;

public class PlayerLifeSystem : MonoBehaviour
{
    [Header("Life Settings")]
    [SerializeField] private int startingLives = 3;

    [Header("Fall Detection")]
    [FormerlySerializedAs("fallDistanceBelowCheckpoint")]
    [SerializeField] private float fallDistance = 5f;

    [Header("User Interface")]
    [SerializeField] private UnityEngine.UI.Text lifeText;

    [Header("Game Over")]
    [SerializeField] private GameOverController gameOverController;

    public int CurrentLives { get; private set; }
    public bool LivesActive { get; private set; }
    public bool IsGameOver => CurrentLives <= 0;

    private Transform respawnPoint;
    private BeginnerPlayerController player;
    private Rigidbody playerBody;
    private float fallStartY;
    private bool fallHandled;

    void Awake()
    {
        CurrentLives = Mathf.Max(1, startingLives);

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
        LivesActive = true;
        fallHandled = false;

        UpdateLifeText();
    }

    void Update()
    {
        if (!LivesActive || IsGameOver)
            return;

        if (player == null || playerBody == null || respawnPoint == null)
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

    public void ActivateLives(
        Transform checkpoint,
        BeginnerPlayerController playerController)
    {
        if (checkpoint == null || playerController == null)
            return;

        respawnPoint = checkpoint;
        player = playerController;
        playerBody = player.GetComponent<Rigidbody>();
        fallStartY = player.transform.position.y;
        LivesActive = true;
        fallHandled = false;

        UpdateLifeText();
    }

    void HandleFall()
    {
        fallHandled = true;

        bool stillAlive = TryLoseLife();

        if (stillAlive)
        {
            player.RespawnAtResetPoint();
        }
        else if (gameOverController != null)
        {
            gameOverController.ShowGameOver(player);
        }
    }

    public bool TryLoseLife()
    {
        if (!LivesActive || IsGameOver)
            return !IsGameOver;

        CurrentLives--;
        UpdateLifeText();

        return CurrentLives > 0;
    }

    public void ResetLives()
    {
        CurrentLives = Mathf.Max(1, startingLives);
        fallHandled = false;

        if (player == null)
            player = FindFirstObjectByType<BeginnerPlayerController>();

        if (player != null)
        {
            playerBody = player.GetComponent<Rigidbody>();
            respawnPoint = player.resetPoint;
            fallStartY = player.transform.position.y;
        }

        LivesActive =
            player != null &&
            playerBody != null &&
            respawnPoint != null;

        UpdateLifeText();
    }

    void UpdateLifeText()
    {
        if (lifeText != null)
            lifeText.text = $"LIVES: {CurrentLives}";
    }
}