using UnityEngine;
using UnityEngine.UI;

public class PlayerProgressUI : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private BeginnerPlayerController player;

    [Header("Text")]
    [SerializeField] private Text heightText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text timeText;
    [SerializeField] private Text checkpointText;

    private float startHeight;
    private float gameTime;
    private int score;
    private float messageTimer;

    void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<BeginnerPlayerController>();

        if (player != null)
            startHeight = player.transform.position.y;

        UpdateScoreText();
        UpdateTimeText();
    }

    void Update()
    {
        if (player != null && heightText != null)
        {
            float currentHeight =
                player.transform.position.y - startHeight;

            currentHeight = Mathf.Max(0f, currentHeight);
            heightText.text = $"HEIGHT: {currentHeight:0.0} m";
        }

        gameTime += Time.deltaTime;
        UpdateTimeText();

        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0f)
                ClearMessage();
        }
    }

    public void AddPoint()
    {
        score++;
        UpdateScoreText();
    }

    void UpdateScoreText()
    {
        if (scoreText != null)
            scoreText.text = $"SCORE: {score}";
    }

    void UpdateTimeText()
    {
        if (timeText == null)
            return;

        int minutes = Mathf.FloorToInt(gameTime / 60f);
        int seconds = Mathf.FloorToInt(gameTime % 60f);
        timeText.text = $"TIME: {minutes:00}:{seconds:00}";
    }

    public void ShowMessage(string message, float seconds = 0f)
    {
        if (checkpointText == null)
            return;

        checkpointText.text = message;
        messageTimer = seconds;
    }

    public void ClearMessage()
    {
        if (checkpointText != null)
            checkpointText.text = "";

        messageTimer = 0f;
    }
}
