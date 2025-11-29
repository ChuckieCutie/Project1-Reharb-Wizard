using UnityEngine;
using TMPro; // Nhớ dùng TextMeshPro
using System.Collections;

public class NumberGame : MonoBehaviour
{
    public TextMeshProUGUI txtTargetNumber; // Kéo UI số cần giơ vào đây
    public TextMeshProUGUI txtFeedback;     // Kéo UI thông báo "Đúng/Sai"
    
    private int targetNum;
    private float holdTime = 0f; // Thời gian giữ đúng thế tay
    private float requiredHoldTime = 1.0f; // Phải giữ đúng 1 giây mới tính

    void Start()
    {
        GenerateNewNumber();
    }

    void Update()
    {
        // Lấy số ngón tay từ Server
        int currentFingers = OpenCapDataManager.Instance.totalFingers;

        if (currentFingers == targetNum)
        {
            // Nếu giơ đúng, bắt đầu đếm giờ
            holdTime += Time.deltaTime;
            txtFeedback.text = $"Giữ nguyên... {holdTime:F1}s";
            txtFeedback.color = Color.yellow;

            if (holdTime >= requiredHoldTime)
            {
                // Đã giữ đủ lâu -> Chiến thắng
                CorrectAnswer();
            }
        }
        else
        {
            // Giơ sai -> Reset bộ đếm
            holdTime = 0;
            txtFeedback.text = $"Bạn đang giơ: {currentFingers}";
            txtFeedback.color = Color.white;
        }
    }

    void GenerateNewNumber()
    {
        targetNum = Random.Range(0, 6); // Random từ 0 đến 5
        txtTargetNumber.text = targetNum.ToString();
        holdTime = 0;
        txtFeedback.text = "Hãy giơ tay theo số!";
    }

    void CorrectAnswer()
    {
        txtFeedback.text = "CHÍNH XÁC!";
        txtFeedback.color = Color.green;
        // Có thể thêm âm thanh hoặc cộng điểm ở đây
        // GameManager.Instance.AddScore(10); 
        
        // Đổi số mới sau 1 giây
        StartCoroutine(WaitAndNext());
    }

    IEnumerator WaitAndNext()
    {
        yield return new WaitForSeconds(1.5f);
        GenerateNewNumber();
    }
}