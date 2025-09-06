using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Runner : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float laneX = 1f;               // Şerit ofseti (yol yarım genişliği)
    [SerializeField] float laneChangeSpeed = 10f;     // Şerit geçiş hızı
    [SerializeField] float gravity = 30f;             // Yerçekimi
    [SerializeField] float forwardSpeed = 6f;         // İleri hız (şimdilik sabit)

    [Header("Animation")]
    [SerializeField] Animator anim;                   // Karakter Animator'u
    [SerializeField] string runParam = "IsRunning";  // Animator bool param adı
    [SerializeField] float maxLeanDegrees = 12f; // Sağ/sola maksimum yatış açısı
    [SerializeField] float leanSpeed = 12f;      // Yatışın tepki hızı
    float currentLeanDeg = 0f;                   // Anlık yatış açısı (Z ekseni)
    float baseYaw;                               // Başlangıç Y rotasyonu (ileri yönü sabitlemek için)

    CharacterController cc;
    // 3 şerit: index 0 = sol, 1 = orta, 2 = sağ
    [SerializeField] int currentLaneIndex = 1; // ortadan başla
    const int minLaneIndex = 0;
    const int maxLaneIndex = 2;
    float verticalVel;
    float currentX;      // x konumunu CharacterController ile taşımak için
    Vector3 startPos;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        startPos = transform.position;
        currentX = transform.position.x;
        currentX = LaneIndexToX(currentLaneIndex);
        baseYaw = transform.localEulerAngles.y;
    }

    void Start()
    {
        if (anim) anim.SetBool(runParam, true); // oyun başında 1 defa idle->run
    }

    void OnEnable()
    {
        MobileInput.OnSwipe += OnSwipe;
    }

    void OnDisable()
    {
        MobileInput.OnSwipe -= OnSwipe;
        if (anim) anim.SetBool(runParam, false);
    }

    void OnSwipe(int dir)
    {
        // dir: -1 = sola kay, +1 = sağa kay; kenarlarda clamp
        int prev = currentLaneIndex;
        currentLaneIndex = Mathf.Clamp(currentLaneIndex + (dir < 0 ? -1 : 1), minLaneIndex, maxLaneIndex);
        if (prev != currentLaneIndex)
            Debug.Log($"[Runner] LaneIndex {prev} -> {currentLaneIndex}");
    }


    void Update()
    {
        if (GameManager.IsPaused) return; // pause iken hareket alma

        // Y ekseni (yerçekimi)
        if (cc.isGrounded) verticalVel = -1f; else verticalVel -= gravity * Time.deltaTime;

        // X ekseni (şerit hedefi)
        float targetX = LaneIndexToX(currentLaneIndex);
        currentX = Mathf.Lerp(currentX, targetX, Time.deltaTime * laneChangeSpeed);
        float deltaX = currentX - transform.position.x; // cc.Move ile taşıyacağız

        // --- Smooth lean (kod tabanlı eğilme) ---
        float leanTargetDeg = 0f;
        float dxNeeded = targetX - transform.position.x; // hedefe yatay uzaklık
        if (Mathf.Abs(laneX) > 0.0001f)
        {
            float ratio = Mathf.Clamp(dxNeeded / laneX, -1f, 1f); // -1..+1
            leanTargetDeg = ratio * maxLeanDegrees;               // hedef yatış
        }
        // Easing
        currentLeanDeg = Mathf.Lerp(currentLeanDeg, leanTargetDeg, Time.deltaTime * leanSpeed);

        // Y yönünü koru, sadece Z ekseninde roll ver
        var e = transform.localEulerAngles;
        e.y = baseYaw;            // ileri yön aynı kalsın
        e.z = -currentLeanDeg;    // sağa giderken sağa yatış (ters gelirse +currentLeanDeg yap)
        transform.localEulerAngles = e;

        // Z ekseni (ileri hareket)
        float zSpeed = forwardSpeed;
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.State == GameState.Running)
                zSpeed = Mathf.Max(forwardSpeed, GameManager.Instance.CurrentSpeed);
            else
                zSpeed = 0f; // Ready/GameOver'da ileri gitme
        }

        // Tek bir cc.Move çağrısıyla vektörü uygula
        Vector3 move = new Vector3(deltaX, verticalVel, zSpeed) * Time.deltaTime;
        cc.Move(move);
    }

    float LaneIndexToX(int idx)
    {
        // 0 -> -laneX, 1 -> 0, 2 -> +laneX
        switch (idx)
        {
            case 0: return -laneX;
            case 2: return +laneX;
            default: return 0f;
        }
    }

    // Skor için erişim kolaylığı
    public float DistanceRun => Vector3.Distance(
        new Vector3(startPos.x, 0, startPos.z),
        new Vector3(transform.position.x, 0, transform.position.z)
    );
}