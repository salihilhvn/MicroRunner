using UnityEngine;

public class Hazard : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[Hazard] Trigger with: {other.name}");
        if (other.CompareTag("Player"))
        {
            Debug.Log("[Hazard] Hit!");
            GameManager.Instance.GameOver();
        }
    }
}