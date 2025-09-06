using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Ready, Running, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] Runner runner;

    [Header("Speed")]
    [SerializeField] float baseSpeed = 6f;
    [SerializeField] float speedRampPerSec = 0.25f;
    [SerializeField] float maxSpeed = 18f;

    public GameState State { get; private set; } = GameState.Ready;
    public float CurrentSpeed { get; private set; }

    float runTime;
    float bestScore;
    float lastScore;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Application.targetFrameRate = 60;
        Time.timeScale = 1f;

        bestScore = PlayerPrefs.GetFloat("BEST_SCORE", 0);
    }

    void Start()
    {
        CurrentSpeed = baseSpeed;
        State = GameState.Running;
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

        if (score > bestScore)
        {
            bestScore = score;
            UIHud.Instance?.SetBest(bestScore);
        }
    }

    public void GameOver()
    {
        if (State == GameState.GameOver) return;
        State = GameState.GameOver;

        // Panelde son koşu ve best'i göster
        UIHud.Instance?.ShowGameOver(true, lastScore, bestScore);

        PlayerPrefs.SetFloat("BEST_SCORE", bestScore);
        Time.timeScale = 0f;
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Pause(bool pause)
    {
        if (State != GameState.Running) return;
        Time.timeScale = pause ? 0f : 1f;
    }
}