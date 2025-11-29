using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public float fallSpeed = 2.0f; // Tốc độ rơi

    void Update()
    {
        // Lệnh này giúp quái vật tự rơi xuống dưới theo thời gian
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

        // (Tùy chọn) Tự hủy nếu rơi quá sâu khỏi màn hình để đỡ nặng máy
        // (Đề phòng trường hợp DangerZone không bắt được va chạm)
        if (transform.position.y < -10f)
        {
            Destroy(gameObject);
        }
    }
}