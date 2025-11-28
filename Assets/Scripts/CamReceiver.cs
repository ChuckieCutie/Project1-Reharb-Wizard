using UnityEngine;
using UnityEngine.UI; // Dùng cho Image thường
using System.Net.Sockets;
using System.Net;
using System.Threading;

public class CamReceiver : MonoBehaviour
{
    [Header("UI Component")]
    public Image displayImage; // Đã đổi từ RawImage sang Image

    [Header("Settings")]
    public int port = 5053; 
    
    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = false;
    
    private byte[] receivedData = null;
    private bool hasNewData = false;
    private Texture2D texture;

    void Start()
    {
        // Khởi tạo Texture
        texture = new Texture2D(320, 240);
        StartUDP();
    }

    void StartUDP()
    {
        try
        {
            udpClient = new UdpClient(port);
            udpClient.Client.ReceiveTimeout = 1000;
            isRunning = true;

            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Lỗi UDP: " + e.Message);
        }
    }

    void ReceiveData()
    {
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
        while (isRunning)
        {
            try
            {
                byte[] data = udpClient.Receive(ref anyIP);
                receivedData = data;
                hasNewData = true;
            }
            catch { }
        }
    }

    void Update()
    {
        if (hasNewData && receivedData != null)
        {
            // 1. Load dữ liệu JPG vào Texture
            texture.LoadImage(receivedData); 
            
            // 2. Tạo Sprite từ Texture để gán vào Image
            Rect rect = new Rect(0, 0, texture.width, texture.height);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            Sprite webCamSprite = Sprite.Create(texture, rect, pivot);

            // 3. Gán vào UI Image
            displayImage.sprite = webCamSprite;

            hasNewData = false;
        }
    }

    void OnDestroy()
    {
        isRunning = false;
        if (udpClient != null) udpClient.Close();
        if (receiveThread != null && receiveThread.IsAlive) receiveThread.Abort();
    }
}