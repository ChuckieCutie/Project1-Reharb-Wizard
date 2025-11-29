using UnityEngine;
using TMPro;

public class SpellCaster : MonoBehaviour
{
    [Header("UI Phản Hồi")]
    public TextMeshProUGUI exercisePrompt;
    public GameObject warningIcon; // Có thể dùng làm icon "Sẵn sàng"

    [Header("Hiệu Ứng")]
    public GameObject explosionPrefab;
    public AudioClip explosionSound;
    private AudioSource audioSource;

    private bool isCharged = false; // Biến kiểm tra đã "nạp đạn" chưa

    void Start()
    {
        if(warningIcon) warningIcon.SetActive(false);
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        // 1. Lấy dữ liệu Nắm/Xòe từ Python
        bool isFist = OpenCapDataManager.Instance.isFist;

        // 2. Logic Game: NẮM ĐỂ NẠP -> XÒE ĐỂ BẮN
        
        if (isFist)
        {
            // Đang nắm tay -> Nạp đạn
            if (!isCharged)
            {
                isCharged = true;
                if(exercisePrompt) exercisePrompt.text = "ĐANG NẮM: Đã nạp đạn! Xòe tay để bắn!";
                if(warningIcon) warningIcon.SetActive(true); // Hiển thị icon sẵn sàng
            }
        }
        else
        {
            // Đang xòe tay -> Nếu đã nạp thì bắn
            if (isCharged)
            {
                ActivateSpell();
                isCharged = false; // Bắn xong phải nắm lại mới được bắn tiếp
                
                if(exercisePrompt) exercisePrompt.text = "ĐANG XÒE: Hãy nắm tay lại!";
                if(warningIcon) warningIcon.SetActive(false); // Tắt icon
            }
        }
    }

    void ActivateSpell()
    {
        // (Giữ nguyên logic tìm và diệt quái cũ)
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length > 0)
        {
            if (explosionSound) audioSource.PlayOneShot(explosionSound);
            foreach (GameObject enemy in enemies)
            {
                if (explosionPrefab) Instantiate(explosionPrefab, enemy.transform.position, Quaternion.identity);
                Destroy(enemy);
            }
            GameManager.Instance.AddScore(100 * enemies.Length);
        }
    }
}