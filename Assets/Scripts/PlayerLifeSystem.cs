using UnityEngine;

public class PlayerLifeSystem : MonoBehaviour
{
    [Header("Life Settings")]
    [SerializeField] private int startingLives = 3;

    [Header("Fall Detection")]
    [Tooltip("The vertical distance below the checkpoint that counts as a fall.")]
    [SerializeField] private float fallDistanceBelowCheckpoint = 4f;

    [Header("User Interface")]
    [SerializeField] private UnityEngine.UI.Text lifeText;

    [Header("Game Over")]
    [SerializeField] private GameOverController gameOverController;

    public int CurrentLives { get; private set; }
    public bool LivesActive { get; private set; }
    public bool IsGameOver => CurrentLives <= 0;

    private Transform activeCheckpoint;
    private BeginnerPlayerController player;
    private bool fallHandled;

    void Awake()
    {
        CurrentLives = Mathf.Max(1, startingLives);

        if (gameOverController == null)
            gameOverController = GetComponent<GameOverController>();

        UpdateLifeText();
    }

    void Update()
    {
        if (!LivesActive || IsGameOver)
            return;

        if (activeCheckpoint == null || player == null)
            return;

        if (fallHandled)
        {
            if (player.transform.position.y
                >= activeCheckpoint.position.y - 1f)
            {
                fallHandled = false;
            }

            return;
        }

        float fallLimit =
            activeCheckpoint.position.y - fallDistanceBelowCheckpoint;

        if (player.transform.position.y < fallLimit)
            HandleFall();
    }

    public void ActivateLives(
        Transform checkpoint,
        BeginnerPlayerController playerController)
    {
        if (checkpoint == null || playerController == null)
            return;

        activeCheckpoint = checkpoint;
        player = playerController;
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
        LivesActive = false;
        activeCheckpoint = null;
        fallHandled = false;

        UpdateLifeText();
    }

    void UpdateLifeText()
    {
        if (lifeText != null)
            lifeText.text = $"LIVES: {CurrentLives}";
    }
}
