using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerCollision : MonoBehaviour
{
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!hit.collider) return;

        // Hazard component'i varsa Game Over
        var hz = hit.collider.GetComponent<Hazard>();
        if (hz != null)
        {
            Debug.Log("[PlayerCollision] Hit Hazard via ControllerColliderHit!");
            GameManager.Instance.GameOver();
        }
    }
}