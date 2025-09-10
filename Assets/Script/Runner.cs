using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Runner : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float laneX = 1f;               // Şerit ofseti (yol yarım genişliği)
    [SerializeField] float laneChangeSpeed = 10f;     // Şerit geçiş hızı
    [SerializeField] float gravity = 30f;             // Yerçekimi (pozitif)
    [SerializeField] float forwardSpeed = 6f;         // İleri hız (şimdilik sabit)

    [Header("Jump")]
    [SerializeField] float jumpHeight = 2.2f;         // zıplama yüksekliği (m)
    [SerializeField] float coyoteTime = 0.10f;        // zeminden ayrıldıktan sonra tolerans
    [SerializeField] float jumpBuffer = 0.10f;        // erken/geç input tamponu
    float lastGroundedTime = -999f;
    float lastJumpPressedTime = -999f;

    // --- Hızlı zıpla & hızlı düş / havada kalmayı kes ---
    [SerializeField] float jumpImpulseMult = 1.10f;   // ilk kalkış çarpanı (1.1–1.3)
    [SerializeField] float riseGravityMult = 1.00f;   // çıkışta yerçekimi katsayısı
    [SerializeField] float fallGravityMult = 3.20f;   // düşüşte yerçekimi katsayısı (sert iniş)

    [SerializeField] float maxAirTime = 0.65f;        // havada kalma üst limiti (sn)
    float airTime = 0f;

    [SerializeField] LayerMask groundMask;            // zemin katmanı (Ground vs)
    [SerializeField] float snapCheckDistance = 0.35f; // zemine yakınken çekme mesafesi

    [Header("Animation")]
    [SerializeField] Animator anim;                   
    [SerializeField] string runParam = "IsRunning";
    [SerializeField] float maxLeanDegrees = 12f;
    [SerializeField] float leanSpeed = 12f;
    float currentLeanDeg = 0f;
    float baseYaw;

    [Header("Animation Params")]
    [SerializeField] string groundedParam = "IsGrounded";
    [SerializeField] string verticalSpeedParam = "VerticalSpeed";
    [SerializeField] string jumpTrigger = "Jump";
    [SerializeField] string landTrigger = "Land";
    bool wasGrounded = true;

    CharacterController cc;
    [SerializeField] int currentLaneIndex = 1; // 0=sol,1=orta,2=sağ
    const int minLaneIndex = 0;
    const int maxLaneIndex = 2;
    float verticalVel;
    float currentX;
    Vector3 startPos;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        startPos = transform.position;
        currentX = LaneIndexToX(currentLaneIndex);
        baseYaw = transform.localEulerAngles.y;
    }

    void Start()
    {
        if (anim) anim.SetBool(runParam, true); // oyun başında idle->run
    }

    void OnEnable()
    {
        MobileInput.OnSwipe += OnSwipe;
        MobileInput.OnJump  += OnJump;
    }

    void OnDisable()
    {
        MobileInput.OnSwipe -= OnSwipe;
        MobileInput.OnJump  -= OnJump;
        if (anim) anim.SetBool(runParam, false);
    }

    void OnSwipe(int dir)
    {
        int prev = currentLaneIndex;
        currentLaneIndex = Mathf.Clamp(currentLaneIndex + (dir < 0 ? -1 : 1), minLaneIndex, maxLaneIndex);
        if (prev != currentLaneIndex)
            Debug.Log($"[Runner] LaneIndex {prev} -> {currentLaneIndex}");
    }

    void OnJump() => lastJumpPressedTime = Time.time;

    void Update()
    {
        if (GameManager.IsPaused) return;

        // Klavye Jump (Space)
        if (Input.GetKeyDown(KeyCode.Space))
            lastJumpPressedTime = Time.time;

        // Grounded takibi
        if (cc.isGrounded)
        {
            if (verticalVel < 0f) verticalVel = -1f; // zemine yapıştır
            lastGroundedTime = Time.time;
            airTime = 0f; // zeminde airtime sıfırla
        }

        // Animator param güncelle
        bool isGroundedNow = cc.isGrounded;
        if (anim)
        {
            anim.SetBool(groundedParam, isGroundedNow);
            anim.SetFloat(verticalSpeedParam, verticalVel);
        }

        // Jump koşulu: buffer + coyote
        bool hasBufferedJump = (Time.time - lastJumpPressedTime) <= jumpBuffer;
        bool coyoteOk        = (Time.time - lastGroundedTime) <= coyoteTime;

        if (hasBufferedJump && (cc.isGrounded || coyoteOk))
        {
            verticalVel = Mathf.Sqrt(2f * gravity * jumpHeight) * jumpImpulseMult; // patlayıcı kalkış
            lastJumpPressedTime = -999f;

            if (anim)
            {
                anim.ResetTrigger(landTrigger);
                anim.SetTrigger(jumpTrigger);
            }
        }

        // Y ekseni (yerçekimi) → hızlı çık / daha hızlı düş + airtime sınırı
        if (!cc.isGrounded)
        {
            airTime += Time.deltaTime;
            float mult = (verticalVel > 0f) ? riseGravityMult : fallGravityMult;

            // fail-safe: havada kalma uzarsa daha da bastır
            if (airTime > maxAirTime) mult *= 1.75f;

            verticalVel -= gravity * mult * Time.deltaTime;
        }

        // Havadan zemine iniş anı → Land trigger
        if (anim && !wasGrounded && isGroundedNow)
        {
            anim.ResetTrigger(jumpTrigger);
            anim.SetTrigger(landTrigger);
        }

        // X ekseni (şerit hedefi)
        float targetX = LaneIndexToX(currentLaneIndex);
        currentX = Mathf.Lerp(currentX, targetX, Time.deltaTime * laneChangeSpeed);
        float deltaX = currentX - transform.position.x;

        // Snap-to-Ground: zemine çok yakınsa indir
        if (verticalVel <= 0f) // sadece düşerken
        {
            Vector3 origin = transform.position + Vector3.up * 0.1f;
            float rayLen = snapCheckDistance + Mathf.Abs(verticalVel) * Time.deltaTime + 0.05f;

            if (Physics.SphereCast(origin, 0.2f, Vector3.down, out RaycastHit hit, rayLen, groundMask, QueryTriggerInteraction.Ignore))
            {
                verticalVel = Mathf.Min(verticalVel, -20f); // daha sert aşağı ivme
                cc.Move(Vector3.down * 0.02f);              // küçük aşağı itme
            }
        }

        // Smooth lean (kod tabanlı eğilme)
        float leanTargetDeg = 0f;
        float dxNeeded = targetX - transform.position.x;
        if (Mathf.Abs(laneX) > 0.0001f)
        {
            float ratio = Mathf.Clamp(dxNeeded / laneX, -1f, 1f);
            leanTargetDeg = ratio * maxLeanDegrees;
        }
        currentLeanDeg = Mathf.Lerp(currentLeanDeg, leanTargetDeg, Time.deltaTime * leanSpeed);

        var e = transform.localEulerAngles;
        e.y = baseYaw;
        e.z = -currentLeanDeg; // sağa giderken sağa yatış
        transform.localEulerAngles = e;

        // Z ekseni (ileri hareket)
        float zSpeed = forwardSpeed;
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.State == GameState.Running)
                zSpeed = Mathf.Max(forwardSpeed, GameManager.Instance.CurrentSpeed);
            else
                zSpeed = 0f;
        }

        // Tek bir cc.Move ile uygula
        Vector3 move = new Vector3(deltaX, verticalVel, zSpeed) * Time.deltaTime;
        cc.Move(move);

        // önceki grounded durumu
        wasGrounded = isGroundedNow;
    }

    float LaneIndexToX(int idx)
    {
        switch (idx)
        {
            case 0: return -laneX;
            case 2: return +laneX;
            default: return 0f;
        }
    }

    public float DistanceRun => Vector3.Distance(
        new Vector3(startPos.x, 0, startPos.z),
        new Vector3(transform.position.x, 0, transform.position.z)
    );
}
