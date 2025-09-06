using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = new Vector3(0, 3.5f, -6f);
    [SerializeField] float smooth = 6f;
    [SerializeField] float tiltAmount = 5f;   // Kamera roll açısı (derece)
    [SerializeField] float tiltSmooth = 4f;   // Kameranın eğilme hızı
    float currentTilt = 0f;

    void LateUpdate()
    {
        if (!target) return;
        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smooth * Time.deltaTime);

        // Kamera tilt (Runner'ın eğilmesine göre)
        float targetTilt = 0f;
        if (target)
            targetTilt = target.localEulerAngles.z;
        // Euler açıları 0–360 olduğu için -180..180 aralığına çek
        if (targetTilt > 180f) targetTilt -= 360f;
        targetTilt = Mathf.Clamp(targetTilt, -15f, 15f); // karakterin eğilmesi aşırıysa sınırla

        float desiredTilt = (targetTilt / 15f) * tiltAmount;
        currentTilt = Mathf.Lerp(currentTilt, desiredTilt, Time.deltaTime * tiltSmooth);

        var rot = transform.rotation.eulerAngles;
        rot.z = -currentTilt; // ters işaret daha doğal görünür, tersine çevir istersen
        transform.rotation = Quaternion.Euler(rot);

        transform.LookAt(target.position + Vector3.forward * 5f);
        // rot.z uygulandıktan sonra tilt korunuyor
    }
}