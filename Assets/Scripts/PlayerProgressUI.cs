using UnityEngine;
using UnityEngine.UI;

public class PlayerProgressUI : MonoBehaviour
{
    public static PlayerProgressUI Instance { get; private set; }

    [Header("Gamer")]
    [SerializeField] private BeginnerPlayerController player;

    [Header("TEXT")]
    [SerializeField] private Text heightText;
    [SerializeField] private Text scoreText;

    private float startHeight;
    private int score;

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

        UpdateScoreText();
    }

    void Update()
    {
        if (player == null || heightText == null)
            return;

        float currentHeight =
            player.transform.position.y - startHeight;

        currentHeight = Mathf.Max(0f, currentHeight);

        heightText.text = $"HEIGHT: {currentHeight:0.0} m";
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
}
