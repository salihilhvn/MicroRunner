using UnityEngine;
using TMPro;

public class UIHud : MonoBehaviour
{
    public static UIHud Instance { get; private set; }

    [Header("Live HUD")]
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI bestText;

    [Header("Game Over Panel")]
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] TextMeshProUGUI finalScoreText;
    [SerializeField] TextMeshProUGUI finalBestText;

    void Awake()
    {
        Instance = this;
        if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    public void SetScore(float meters)
    {
        if (scoreText) scoreText.text = $"{meters:0} m";
    }

    public void SetBest(float meters)
    {
        if (bestText) bestText.text = $"BEST: {meters:0} m";
    }

    // Paneli aç ve final değerleri yaz
    public void ShowGameOver(bool show, float lastRunMeters = 0f, float bestMeters = 0f)
    {
        if (!gameOverPanel) return;
        gameOverPanel.SetActive(show);
        if (show)
        {
            if (finalScoreText) finalScoreText.text = $"SCORE: {lastRunMeters:0} m";
            if (finalBestText)  finalBestText.text  = $"BEST:  {bestMeters:0} m";
        }
    }

    // UI Button Events
    public void OnRetry()  => GameManager.Instance.Retry();
    public void OnPause()  => GameManager.Instance.Pause(true);
    public void OnResume() => GameManager.Instance.Pause(false);
}