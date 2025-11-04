using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;

// Đây là một Singleton, nghĩa là nó chỉ tồn tại 1 lần
// và các script khác có thể gọi nó
public class OpenCapDataManager : MonoBehaviour
{
    public static OpenCapDataManager Instance { get; private set; }

    // Dữ liệu thời gian thực từ Python
    public float currentShoulderAngle = 0f;
    public float currentTrunkLean = 0f;

    private TcpClient client;
    private StreamReader reader;
    private bool isConnected = false;

    private void Awake()
    {
        // Thiết lập Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Giữ nó tồn tại khi chuyển scene
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Chạy kết nối trong một luồng (thread) riêng để không làm treo game
        ConnectToServer();
    }

    private void ConnectToServer()
    {
        Task.Run(() =>
        {
            try
            {
                client = new TcpClient("127.0.0.1", 5000);
                reader = new StreamReader(client.GetStream(), Encoding.UTF8);
                isConnected = true;
                Debug.Log("--- CLIENT: Đã kết nối tới Server Python! ---");

                // Vòng lặp nhận dữ liệu
                while (isConnected && client.Connected)
                {
                    string message = reader.ReadLine(); // Đọc cho đến khi gặp \n
                    if (message != null)
                    {
                        // Parse JSON và cập nhật vào biến
                        PoseData data = JsonUtility.FromJson<PoseData>(message);
                        currentShoulderAngle = data.rom;
                        currentTrunkLean = data.comp;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Lỗi Socket: {e.Message}");
                isConnected = false;
            }
        });
    }

    void OnDestroy()
    {
        // Dọn dẹp khi game tắt
        isConnected = false;
        if (reader != null) reader.Close();
        if (client != null) client.Close();
    }
}