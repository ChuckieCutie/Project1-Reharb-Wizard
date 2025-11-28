using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Globalization;

public class OpenCapDataManager : MonoBehaviour
{
    public static OpenCapDataManager Instance { get; private set; }

    [Header("Network Settings")]
    public int port = 5052; // Cổng dữ liệu xương khớp (khớp với Python)
    
    [Header("Hand Status")]
    // Biến này để SpellCaster.cs đọc
    public volatile bool isFist = false; 
    
    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = false;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        StartServer();
    }

    private void StartServer()
    {
        try 
        {
            // Kill UDP cũ nếu còn kẹt
            if (udpClient != null) udpClient.Close();

            udpClient = new UdpClient(port);
            udpClient.Client.ReceiveTimeout = 1000; 
            
            isRunning = true;
            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
            
            Debug.Log($"<color=green>--- UDP LISTENING ON PORT {port} ---</color>");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to start UDP: " + e.Message);
        }
    }

    private void ReceiveData()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

        while (isRunning)
        {
            try
            {
                // 1. Nhận dữ liệu thô từ Python
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string jsonString = Encoding.UTF8.GetString(data);

                // 2. Parse lấy tọa độ bàn tay (Thủ công để tránh lỗi JsonUtility)
                if (jsonString.Contains("\"x\":"))
                {
                    List<Vector3> handPoints = ParseFirstHand(jsonString);
                    
                    // 3. Nếu tìm thấy đủ 21 điểm xương tay -> Tính toán nắm/xòe
                    if (handPoints != null && handPoints.Count == 21)
                    {
                        isFist = CheckIsFist(handPoints);
                    }
                }
            }
            catch (SocketException) 
            {
                // Timeout, bỏ qua
            }
            catch (Exception e)
            {
                Debug.LogWarning("UDP Error: " + e.Message);
            }
        }
    }

    // --- THUẬT TOÁN KIỂM TRA NẮM TAY ---
    private bool CheckIsFist(List<Vector3> landmarks)
    {
        // Logic: So sánh khoảng cách từ đầu ngón (Tip) đến Cổ tay (0)
        // Nếu đầu ngón gần cổ tay hơn khớp nối (MCP) -> Ngón đang gập
        
        Vector3 wrist = landmarks[0];
        
        // Các điểm đầu ngón: 8(Trỏ), 12(Giữa), 16(Nhẫn), 20(Út)
        // Các khớp nối tương ứng: 5, 9, 13, 17
        
        bool indexFolded  = Vector3.Distance(landmarks[8], wrist)  < Vector3.Distance(landmarks[5], wrist);
        bool middleFolded = Vector3.Distance(landmarks[12], wrist) < Vector3.Distance(landmarks[9], wrist);
        bool ringFolded   = Vector3.Distance(landmarks[16], wrist) < Vector3.Distance(landmarks[13], wrist);
        bool pinkyFolded  = Vector3.Distance(landmarks[20], wrist) < Vector3.Distance(landmarks[17], wrist);

        // Nếu 3 trên 4 ngón dài đang gập -> Coi là nắm tay
        int foldedCount = (indexFolded ? 1 : 0) + (middleFolded ? 1 : 0) + (ringFolded ? 1 : 0) + (pinkyFolded ? 1 : 0);
        
        return foldedCount >= 3;
    }

    // --- HÀM TÁCH JSON THỦ CÔNG (Chạy nhanh, không cần thư viện) ---
    private List<Vector3> ParseFirstHand(string json)
    {
        List<Vector3> points = new List<Vector3>();
        
        // Cắt chuỗi theo từ khóa "x": để lấy từng điểm
        string[] rawPoints = json.Split(new string[] { "{\"x\": " }, StringSplitOptions.RemoveEmptyEntries);

        // Bỏ phần tử đầu (rác header), lấy tối đa 21 điểm
        for (int i = 1; i < rawPoints.Length && i <= 21; i++)
        {
            try 
            {
                string p = rawPoints[i];
                
                float x = ParseValue(p, ",");
                
                int yIdx = p.IndexOf("\"y\":") + 4;
                float y = ParseValue(p.Substring(yIdx), ",");

                int zIdx = p.IndexOf("\"z\":") + 4;
                float z = ParseValue(p.Substring(zIdx), "}");

                points.Add(new Vector3(x, y, z));
            }
            catch {}
        }
        return points;
    }

    private float ParseValue(string str, string endChar)
    {
        int end = str.IndexOf(endChar);
        if (end == -1) return 0;
        string val = str.Substring(0, end).Trim();
        return float.Parse(val, CultureInfo.InvariantCulture);
    }

    void OnDestroy()
    {
        isRunning = false;
        if (udpClient != null) udpClient.Close();
        if (receiveThread != null && receiveThread.IsAlive) receiveThread.Abort();
    }
}