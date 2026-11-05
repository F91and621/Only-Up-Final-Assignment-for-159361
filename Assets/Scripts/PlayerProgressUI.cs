using UnityEngine;
using UnityEngine.UI;

public class PlayerProgressUI : MonoBehaviour
{
    public static PlayerProgressUI Instance { get; private set; }

    [SerializeField] private BeginnerPlayerController player;
    [SerializeField] private Text heightText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text timeText;

    private float startHeight;
    private int score;
    private float gameTime;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<BeginnerPlayerController>();
        if (player != null)
            startHeight = player.transform.position.y;

        SetTextStyle(heightText);
        SetTextStyle(scoreText);
        SetTextStyle(timeText);
        UpdateScoreText();
    }

    void Update()
    {
        gameTime += Time.deltaTime;
        UpdateTimeText();
        if (player == null || heightText == null)
            return;

        float height = player.transform.position.y - startHeight;
        heightText.text = $"HEIGHT: {Mathf.Max(0f, height):0.0} m";
    }

    public void AddPoint()
    {
        score++;
        UpdateScoreText();
    }

    public float GetGameTime()
    {
        return gameTime;
    }

    void SetTextStyle(Text text)
    {
        if (text == null)
            return;
        text.color = Color.black;
        text.fontStyle = FontStyle.Bold;
        text.fontSize = 34;
        text.alignment = TextAnchor.MiddleLeft;
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
}
