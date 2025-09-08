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
    [SerializeField] TextMeshProUGUI finalScoreText;  // kutu içi SCORE değeri (sadece "X m")
    [SerializeField] TextMeshProUGUI finalBestText;   // kutu içi BEST değeri  (sadece "Y m")

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

        // Pause butonu GameOver panelinin altında kalmasın
        if (pauseButton && gameOverPanel)
        {
            if (pauseButton.transform.IsChildOf(gameOverPanel.transform))
            {
                var pauseRT = pauseButton.GetComponent<RectTransform>();
                var panelParent = gameOverPanel.transform.parent as RectTransform;
                if (pauseRT != null && panelParent != null)
                    pauseRT.SetParent(panelParent, false);
            }
        }

        if (!pauseLabel && pauseButton)
            pauseLabel = pauseButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (pauseLabel) pauseLabel.text = "Pause";
    }
    void Start()
    {
        // All-time göstereceksen:
        // bestText.text = $"{GameManager.Instance?.BestScore ?? 0f:0} m";
        // Session-best için:
        bestText.text = "0 m";
    }

    // Canlı skor/best (üstteki kutular)
    public void SetScore(float meters)
    {
        if (scoreText) scoreText.text = $"{meters:0} m";
    }

    public void SetBest(float meters)
    {
        // Üstteki BEST kutusunun içine sadece "Y m" yazıyoruz
        bestText.text = $"{(GameManager.Instance ? GameManager.Instance.BestScore : 0f):0} m";
    }

    // Game Over: kutuların içine SADECE "X m" / "Y m" yaz
    public void ShowGameOver(bool show, float lastRunMeters = 0f, float bestMeters = 0f)
    {
        if (!gameOverPanel) return;

        gameOverPanel.SetActive(show);

        if (show)
        {
            // final kutular – ön ek yok, sadece değer + " m"
            if (finalScoreText) finalScoreText.text = $"{lastRunMeters:0} m";
            if (finalBestText)  finalBestText.text  = $"{bestMeters:0} m";

            if (retryButton)  retryButton.gameObject.SetActive(true);
            if (resumeButton) resumeButton.gameObject.SetActive(false);
            if (pauseButton)  pauseButton.gameObject.SetActive(false);
        }
        else
        {
            if (retryButton)  retryButton.gameObject.SetActive(false);
            if (resumeButton) resumeButton.gameObject.SetActive(false);
            if (pauseButton)  pauseButton.gameObject.SetActive(true);
        }
    }

    // === UI Button Events ===
    public void OnRetry()  => GameManager.Instance.Retry();

    public void OnPause()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.State == GameState.GameOver) return;

        bool willPause = !GameManager.IsPaused;
        GameManager.Instance.Pause(willPause);
        if (pauseLabel) pauseLabel.text = willPause ? "Resume" : "Pause";

        if (!willPause)
        {
            if (gameOverPanel) gameOverPanel.SetActive(false);
            if (retryButton)  retryButton.gameObject.SetActive(false);
            if (resumeButton) resumeButton.gameObject.SetActive(false);
        }
    }

    public void OnResume()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.State == GameState.GameOver) return;

        GameManager.Instance.Pause(false);
        if (pauseLabel) pauseLabel.text = "Pause";
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (retryButton)  retryButton.gameObject.SetActive(false);
        if (resumeButton) resumeButton.gameObject.SetActive(false);
    }

    // === BEST temizleme (hızlı çözüm) ===
    // Bunu bir butonun OnClick'ine bağlayabilirsin.
    public void OnClearBest()
    {
        GameManager.Instance?.ResetBestScore();
    }

}
