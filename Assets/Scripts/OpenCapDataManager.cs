using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

public class OpenCapDataManager : MonoBehaviour
{
    public static OpenCapDataManager Instance { get; private set; }

    // Biến mới: Trạng thái tay (True = Nắm, False = Xòe)
    public volatile bool isFist = false;

    private TcpClient client;
    private StreamReader reader;
    private bool isRunning = false;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        isRunning = true;
        ConnectToServer();
    }

    private void ConnectToServer()
    {
        Task.Run(() =>
        {
            while (isRunning)
            {
                try
                {
                    if (client == null || !client.Connected)
                    {
                        client = new TcpClient();
                        var result = client.BeginConnect("127.0.0.1", 5000, null, null);
                        bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));

                        if (success)
                        {
                            client.EndConnect(result);
                            reader = new StreamReader(client.GetStream(), Encoding.UTF8);
                            Debug.Log("<color=green>--- ĐÃ KẾT NỐI VỚI PYTHON HANDS ---</color>");
                        }
                        else { client.Close(); }
                    }

                    if (client != null && client.Connected)
                    {
                        string message = reader.ReadLine();
                        if (message != null)
                        {
                            PoseData data = JsonUtility.FromJson<PoseData>(message);
                            if (data != null)
                            {
                                isFist = data.isFist; // Cập nhật trạng thái nắm tay
                            }
                        }
                        else { client.Close(); }
                    }
                }
                catch (Exception) { if (client != null) client.Close(); }
                Thread.Sleep(50); // Giảm delay xuống 50ms cho nhạy hơn với thao tác tay
            }
        });
    }

    void OnDestroy()
    {
        isRunning = false;
        if (reader != null) reader.Close();
        if (client != null) client.Close();
    }
}