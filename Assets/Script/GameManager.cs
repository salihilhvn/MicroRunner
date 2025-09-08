using UnityEngine;
using UnityEngine.SceneManagement;
using System; // for Action<>

public enum GameState { Ready, Running, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public float DistanceRun => runner ? runner.DistanceRun : 0f;
    public float BestScore   => bestScore;

    [Header("Refs")]
    [SerializeField] Runner runner;

    [Header("Speed")]
    [SerializeField] float baseSpeed = 6f;
    [SerializeField] float speedRampPerSec = 0.25f;
    [SerializeField] float maxSpeed = 18f;
    // --- GameManager alanlarına ekle ---
    [SerializeField] bool bestMirrorsScoreAfterSurpass = true;          // BEST, aşıldıktan sonra score ile akar
    [SerializeField] bool overwriteBestWithLastRunOnGameOver = true;     // GameOver’da BEST = lastRun (düşük/eşit/yüksek fark etmez)

// (var olan alanlar aynı)


    public GameState State { get; private set; } = GameState.Ready;
    public float CurrentSpeed { get; private set; }
    
    float runTime;
    float bestScore;
    float lastScore;
    float sessionBest; // bu koşunun en iyisi (HUD’a yazmayacağız)

    public static bool IsPaused { get; private set; }
    public static event Action<bool> OnPauseStateChanged;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Application.targetFrameRate = 60;
        Time.timeScale = 1f;
        IsPaused = false; // reset any leftover pause state on scene reload

        bestScore = PlayerPrefs.GetFloat("BEST_SCORE", 0);
    }

    void Start()
    {
        CurrentSpeed = baseSpeed;
        State = GameState.Running;

        sessionBest = 0f; // dursun, HUD’a yazmıyoruz
        // Başlangıçta HUD’da all-time best göster
        UIHud.Instance?.SetBest(bestScore);
    }


    void Update()
    {
        if (State != GameState.Running) return;

        runTime += Time.deltaTime;
        CurrentSpeed = Mathf.Min(maxSpeed, baseSpeed + speedRampPerSec * runTime);

        float score = runner ? runner.DistanceRun : 0f;
        lastScore = score;
        UIHud.Instance?.SetScore(score);

        // İçeride sessionBest tutabilirsin (HUD’a yazmıyoruz)
        if (score > sessionBest) sessionBest = score;

        // 1) KALICI BEST’i güncelle (klasik “best” mantığı)
        if (score > bestScore)
            bestScore = score;

        // 2) Ekranda gözükecek BEST (displayBest):
        //    - Geçilene kadar all-time best
        //    - Geçildikten sonra score ile birlikte ak
        float displayBest = bestMirrorsScoreAfterSurpass
            ? Mathf.Max(bestScore, score)
            : bestScore;

        UIHud.Instance?.SetBest(displayBest);
    }


    public void GameOver()
    {
        if (State == GameState.GameOver) return;
        State = GameState.GameOver;
        IsPaused = true;
        OnPauseStateChanged?.Invoke(true);

        // BEST asla düşmez:
        bestScore = Mathf.Max(bestScore, lastScore);

        PlayerPrefs.SetFloat("BEST_SCORE", bestScore);
        PlayerPrefs.Save();

        UIHud.Instance?.ShowGameOver(true, lastScore, bestScore);
        Time.timeScale = 0f;
    }



    public void Retry()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        sessionBest = 0f; // yeni koşu için sıfırla (HUD’a yazmıyoruz)
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Pause(bool pause)
    {
        // Only allow pausing/resuming during active gameplay
        if (State != GameState.Running) return;

        IsPaused = pause;
        Time.timeScale = pause ? 0f : 1f;
        OnPauseStateChanged?.Invoke(pause);
    }

    public void ResetBestScore()
    {
        bestScore = 0f;
        PlayerPrefs.DeleteKey("BEST_SCORE");
        PlayerPrefs.Save();                     // --- ADD: kalıcı kaydı hemen temizle ---
        UIHud.Instance?.SetBest(0f);            // canlı HUD’ı da anında güncelle
    }
}
    
