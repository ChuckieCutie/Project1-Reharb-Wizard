using UnityEngine;
using TMPro; // Dùng để hiển thị text

public class SpellCaster : MonoBehaviour
{
    [Header("Ngưỡng Trị Liệu")]
    public float romThreshold = 80.0f; // Phải giơ tay cao hơn 80 độ
    public float compensationThreshold = 15.0f; // Nghiêng thân > 15 độ là sai

    [Header("UI Phản Hồi")]
    public TextMeshProUGUI exercisePrompt; // Text hướng dẫn
    public GameObject warningIcon; // Icon/Text cảnh báo

    private bool isSpellReady = true; // Cờ để tránh spam phép

    void Start()
    {
        warningIcon.SetActive(false); // Ẩn cảnh báo
        exercisePrompt.text = "Bài tập: GIƠ TAY NGANG!";
    }

    void Update()
    {
        // 1. Lấy dữ liệu từ Singleton
        float rom = OpenCapDataManager.Instance.currentShoulderAngle;
        float comp = Mathf.Abs(OpenCapDataManager.Instance.currentTrunkLean); // Lấy trị tuyệt đối

        // 2. Kiểm tra Bù Trừ (luôn luôn kiểm tra)
        if (comp > compensationThreshold)
        {
            warningIcon.SetActive(true);
        }
        else
        {
            warningIcon.SetActive(false);
        }

        // 3. Kiểm tra Kích hoạt Phép
        // Điều kiện: Sẵn sàng + Đạt ROM + KHÔNG Bù trừ
        if (isSpellReady && rom > romThreshold && comp < compensationThreshold)
        {
            Debug.Log("PHÉP KÍCH HOẠT!");
            ActivateSpell();
            isSpellReady = false; // Phải hạ tay xuống mới được làm lại
        }

        // 4. Kiểm tra Reset Phép (Khi người chơi hạ tay xuống)
        if (!isSpellReady && rom < 30.0f) // Hạ tay về dưới 30 độ
        {
            isSpellReady = true;
        }
    }

    void ActivateSpell()
    {
        // Tìm TẤT CẢ kẻ thù đang có
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemy in enemies)
        {
            // (Tùy chọn: Thêm hiệu ứng nổ ở đây)
            Destroy(enemy);
        }

        // Cộng điểm
        GameManager.Instance.AddScore(100 * enemies.Length); // Tiêu diệt càng nhiều, điểm càng cao
    }
}