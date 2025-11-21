using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Để chơi lại

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject gameOverPanel; 
    
    public TextMeshProUGUI txtScore;
private int score = 0;

public void AddScore(int amount)
{
    score += amount;
    txtScore.text = "Score: " + score;
}// Kéo Panel UI vào đây

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        gameOverPanel.SetActive(false); // Ẩn UI thua lúc đầu
        Time.timeScale = 1; // Đảm bảo game chạy
    }
    

    public void GameOver()
    {
        Debug.Log("GAME OVER!");
        Time.timeScale = 0; // Dừng game
        gameOverPanel.SetActive(true); // Hiển thị UI thua
    }

    public void RestartGame()
    {
        Time.timeScale = 1; // Chạy lại
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}