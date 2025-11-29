using UnityEngine;

public class DangerZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Nếu là kẻ thù chạm vào
        if (other.CompareTag("Enemy"))
        {
            GameManager.Instance.GameOver();
        }
    }
}