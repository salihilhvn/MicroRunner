using UnityEngine;
using TMPro;
using UnityEngine.UI;

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

    [Header("Buttons")]
    [SerializeField] Button retryButton;
    [SerializeField] Button resumeButton;
    [SerializeField] Button pauseButton;
    [SerializeField] TextMeshProUGUI pauseLabel;

    void Awake()
    {
        Instance = this;
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (retryButton)  retryButton.gameObject.SetActive(false);
        if (resumeButton) resumeButton.gameObject.SetActive(false);
        if (pauseButton)  pauseButton.gameObject.SetActive(true);

        // Ensure Pause button is ALWAYS visible in gameplay: if it's under the GameOver panel, re-parent it to the panel's parent
        if (pauseButton && gameOverPanel)
        {
            if (pauseButton.transform.IsChildOf(gameOverPanel.transform))
            {
                var pauseRT = pauseButton.GetComponent<RectTransform>();
                var panelParent = gameOverPanel.transform.parent as RectTransform;
                if (pauseRT != null && panelParent != null)
                {
                    // Re-parent without keeping world pos so it preserves local anchors nicely
                    pauseRT.SetParent(panelParent, false);
                }
            }
        }

        // Auto-find pause label if not assigned
        if (!pauseLabel && pauseButton)
            pauseLabel = pauseButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (pauseLabel) pauseLabel.text = "Pause";
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
            if (retryButton)  retryButton.gameObject.SetActive(true);
            if (resumeButton) resumeButton.gameObject.SetActive(false);
            if (pauseButton)  pauseButton.gameObject.SetActive(false);
        }
        else
        {
            if (retryButton)  retryButton.gameObject.SetActive(false);
            if (resumeButton) resumeButton.gameObject.SetActive(false);
            // pauseButton remains as-is (always visible)
            if (pauseButton)  pauseButton.gameObject.SetActive(true);
        }
    }

    // UI Button Events
    public void OnRetry()  => GameManager.Instance.Retry();
    public void OnPause()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.State == GameState.GameOver)
        {
            Debug.Log("[UIHud] OnPause ignored — GameOver state");
            return;
        }

        bool willPause = !GameManager.IsPaused; // toggle pause
        GameManager.Instance.Pause(willPause);  // updates flag, timeScale, and raises event

        if (pauseLabel) pauseLabel.text = willPause ? "Resume" : "Pause";

        if (!willPause)
        {
            // resuming gameplay
            if (gameOverPanel) gameOverPanel.SetActive(false);
            if (retryButton)  retryButton.gameObject.SetActive(false);
            if (resumeButton) resumeButton.gameObject.SetActive(false);
        }

        Debug.Log(willPause ? "[UIHud] Paused" : "[UIHud] Resumed");
    }
    public void OnResume()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.State == GameState.GameOver)
        {
            Debug.Log("[UIHud] Resume ignored — GameOver state");
            return;
        }

        GameManager.Instance.Pause(false); // force resume
        if (pauseLabel) pauseLabel.text = "Pause";
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (retryButton)  retryButton.gameObject.SetActive(false);
        if (resumeButton) resumeButton.gameObject.SetActive(false);
        Debug.Log("[UIHud] Resumed");
    }
}