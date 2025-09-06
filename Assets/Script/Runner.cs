using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Runner : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float laneX = 1f;               // Şerit ofseti (yol yarım genişliği)
    [SerializeField] float laneChangeSpeed = 10f;     // Şerit geçiş hızı
    [SerializeField] float gravity = 30f;             // Yerçekimi
    [SerializeField] float forwardSpeed = 6f;         // İleri hız (şimdilik sabit)

    CharacterController cc;
    int currentLane = 0; // -1 veya +1 olacak
    float verticalVel;
    float currentX;      // x konumunu CharacterController ile taşımak için
    Vector3 startPos;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        startPos = transform.position;
        currentX = transform.position.x;
    }

    void OnEnable()
    {
        // Tap/click geldiğinde şerit değiştir
        MobileInput.OnTap += SwitchLane;
    }

    void OnDisable()
    {
        MobileInput.OnTap -= SwitchLane;
    }

    void SwitchLane()
    {
        // -1 <-> +1 arasında toggle
        currentLane = (currentLane == -1) ? 1 : -1;
    }

    void Update()
    {
        // Y ekseni (yerçekimi)
        if (cc.isGrounded) verticalVel = -1f; else verticalVel -= gravity * Time.deltaTime;

        // X ekseni (şerit hedefi)
        float targetX = currentLane * laneX;
        currentX = Mathf.Lerp(currentX, targetX, Time.deltaTime * laneChangeSpeed);
        float deltaX = currentX - transform.position.x; // cc.Move ile taşıyacağız

        // Z ekseni (ileri hareket)
        float zSpeed = forwardSpeed;

        // Tek bir cc.Move çağrısıyla vektörü uygula
        Vector3 move = new Vector3(deltaX, verticalVel, zSpeed) * Time.deltaTime;
        cc.Move(move);
    }

    // Skor için erişim kolaylığı
    public float DistanceRun => Vector3.Distance(
        new Vector3(startPos.x, 0, startPos.z),
        new Vector3(transform.position.x, 0, transform.position.z)
    );
}