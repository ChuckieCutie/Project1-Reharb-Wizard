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
    public int port = 5052; // Cổng dữ liệu xương khớp
    
    [Header("Hand Status")]
    public volatile int totalFingers = 0; // Tổng số ngón của TẤT CẢ bàn tay
    public volatile bool isFist = false;  // Ít nhất 1 tay đang nắm

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
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string jsonString = Encoding.UTF8.GetString(data);

                // 1. Lấy tất cả điểm xương từ JSON
                List<Vector3> allPoints = ParseAllPoints(jsonString);
                
                // 2. Tính toán (Mỗi bàn tay có 21 điểm)
                if (allPoints != null && allPoints.Count > 0)
                {
                    int tempTotalFingers = 0;
                    bool tempIsFist = false;

                    // Duyệt từng cụm 21 điểm (tương ứng 1 bàn tay)
                    for (int i = 0; i < allPoints.Count; i += 21)
                    {
                        // Đảm bảo đủ 1 bàn tay (tránh lỗi array index)
                        if (i + 21 <= allPoints.Count)
                        {
                            // Cắt lấy 21 điểm của bàn tay hiện tại
                            List<Vector3> handLandmarks = allPoints.GetRange(i, 21);
                            
                            int f = CountFingersPerHand(handLandmarks);
                            tempTotalFingers += f;

                            if (CheckIsFist(handLandmarks)) tempIsFist = true;
                        }
                    }

                    // Cập nhật biến global
                    totalFingers = tempTotalFingers;
                    isFist = tempIsFist;
                }
                else
                {
                    totalFingers = 0;
                }
            }
            catch (Exception) { }
        }
    }

    // --- THUẬT TOÁN ĐẾM NGÓN (CẢI TIẾN) ---
    private int CountFingersPerHand(List<Vector3> landmarks)
    {
        int count = 0;
        Vector3 wrist = landmarks[0];

        // 1. Kiểm tra 4 ngón dài (Trỏ, Giữa, Nhẫn, Út)
        // Logic: Đầu ngón (Tip) xa Cổ tay (Wrist) hơn Khớp nối (MCP) -> Duỗi
        if (Vector3.Distance(landmarks[8], wrist) > Vector3.Distance(landmarks[5], wrist)) count++;  // Trỏ
        if (Vector3.Distance(landmarks[12], wrist) > Vector3.Distance(landmarks[9], wrist)) count++; // Giữa
        if (Vector3.Distance(landmarks[16], wrist) > Vector3.Distance(landmarks[13], wrist)) count++; // Nhẫn
        if (Vector3.Distance(landmarks[20], wrist) > Vector3.Distance(landmarks[17], wrist)) count++; // Út

        // 2. Kiểm tra Ngón Cái (FIX LỖI SỐ 4)
        // Logic cũ: So Tip vs MCP với Wrist -> Hay bị sai khi khép tay.
        // Logic mới: So sánh khoảng cách Ngón Cái (4) đến Gốc Ngón Út (17)
        // Nếu Cái duỗi ra, nó sẽ XA ngón út. Nếu cụp vào, nó sẽ GẦN ngón út.
        
        float thumbOutThreshold = Vector3.Distance(landmarks[17], landmarks[2]); // Lấy thước đo là độ rộng lòng bàn tay
        if (Vector3.Distance(landmarks[4], landmarks[17]) > thumbOutThreshold * 0.9f) // 0.9 là hệ số du di
        {
            count++;
        }

        return count;
    }

    private bool CheckIsFist(List<Vector3> landmarks)
    {
        // Nắm tay là khi ngón trỏ, giữa, nhẫn, út đều gập
        Vector3 wrist = landmarks[0];
        bool indexFolded  = Vector3.Distance(landmarks[8], wrist)  < Vector3.Distance(landmarks[5], wrist);
        bool middleFolded = Vector3.Distance(landmarks[12], wrist) < Vector3.Distance(landmarks[9], wrist);
        bool ringFolded   = Vector3.Distance(landmarks[16], wrist) < Vector3.Distance(landmarks[13], wrist);
        bool pinkyFolded  = Vector3.Distance(landmarks[20], wrist) < Vector3.Distance(landmarks[17], wrist);

        int foldedCount = (indexFolded ? 1 : 0) + (middleFolded ? 1 : 0) + (ringFolded ? 1 : 0) + (pinkyFolded ? 1 : 0);
        return foldedCount >= 3;
    }

    // --- PARSING JSON (LẤY TẤT CẢ ĐIỂM) ---
    private List<Vector3> ParseAllPoints(string json)
    {
        List<Vector3> points = new List<Vector3>();
        string[] rawPoints = json.Split(new string[] { "{\"x\": " }, StringSplitOptions.RemoveEmptyEntries);

        // Bỏ phần tử 0 (header rác), lấy tất cả còn lại
        for (int i = 1; i < rawPoints.Length; i++)
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