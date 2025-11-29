using UnityEngine;
using System.Diagnostics;
using System.IO;

public class ServerLauncher : MonoBehaviour
{
    [Header("Cấu hình Python")]
    // Nếu bạn cài Python bình thường và đã thêm vào PATH thì để "python"
    // Nếu dùng Anaconda hoặc Env riêng, hãy dán đường dẫn full (VD: C:\Anaconda3\python.exe)
    public string pythonExe = "python"; 
    
    // Tên file script (phải nằm trong thư mục RehabServer ngang hàng với Assets)
    public string scriptName = "server.py";
    
    [Header("Debug")]
    public bool showConsoleWindow = false; // Bật true nếu muốn hiện cửa sổ đen CMD để debug

    private Process serverProcess;

    void Start()
    {
        // Chỉ tự động chạy Server khi đang ở trong Unity Editor
        #if UNITY_EDITOR
            RunPythonScript();
        #else
            UnityEngine.Debug.Log("💡 Đang ở chế độ Build Game: Hãy chạy file .exe server thủ công.");
        #endif
    }

    void RunPythonScript()
    {
        // Tìm đường dẫn file server.py
        // Application.dataPath trả về folder "Assets", ta lùi ra 1 cấp để vào folder dự án
        string projectPath = Directory.GetParent(Application.dataPath).FullName;
        string scriptPath = Path.Combine(projectPath, "RehabServer", scriptName);

        if (!File.Exists(scriptPath))
        {
            UnityEngine.Debug.LogError($"❌ Không tìm thấy file Python tại: {scriptPath}");
            return;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = pythonExe;
        
        // Thêm tham số "-u" để unbuffered output (log hiện ngay lập tức không bị trễ)
        startInfo.Arguments = $"-u \"{scriptPath}\""; 
        
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = !showConsoleWindow; // Ẩn cửa sổ đen nếu không cần thiết
        
        // Redirect luồng xuất/nhập để đọc log ngay trong Unity Console
        startInfo.RedirectStandardOutput = true; 
        startInfo.RedirectStandardError = true;

        try
        {
            serverProcess = Process.Start(startInfo);
            UnityEngine.Debug.Log($"<color=green>✅ Đã bật Python Server: {scriptPath}</color>");
            
            // --- XỬ LÝ LOG THÔNG MINH ---
            
            // 1. Log thông thường (print từ python)
            serverProcess.OutputDataReceived += (sender, args) => 
            { 
                if (!string.IsNullOrEmpty(args.Data)) 
                    UnityEngine.Debug.Log($"[PY]: {args.Data}"); 
            };

            // 2. Log lỗi (stderr) - Lọc bớt các cảnh báo không cần thiết của TensorFlow/MediaPipe
            serverProcess.ErrorDataReceived += (sender, args) => 
            { 
                if (!string.IsNullOrEmpty(args.Data)) 
                {
                    string msg = args.Data;
                    // Nếu log chứa từ khóa Warning hoặc Info thì chỉ Log Vàng (Warning) thay vì Đỏ (Error)
                    if (msg.Contains("WARNING") || msg.Contains("INFO") || 
                        msg.Contains("UserWarning") || msg.Contains("deprecated") || 
                        msg.Contains("Feedback manager"))
                    {
                        UnityEngine.Debug.LogWarning($"[PY WARN]: {msg}");
                    }
                    else
                    {
                        // Lỗi nghiêm trọng mới báo đỏ
                        UnityEngine.Debug.LogError($"[PY ERROR]: {msg}");
                    }
                }
            };
            
            serverProcess.BeginOutputReadLine();
            serverProcess.BeginErrorReadLine();
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("❌ Lỗi khởi động Python. Hãy kiểm tra lại đường dẫn python.exe! \nLỗi: " + e.Message);
        }
    }

    // Đảm bảo tắt server khi tắt Unity
    void OnApplicationQuit()
    {
        KillServer();
    }

    void OnDestroy()
    {
        KillServer();
    }

    void KillServer()
    {
        if (serverProcess != null && !serverProcess.HasExited)
        {
            try 
            {
                serverProcess.Kill();
                serverProcess.Dispose();
                serverProcess = null;
                UnityEngine.Debug.Log("🛑 Đã tắt Python Server.");
            }
            catch (System.Exception e)
            {
                // Đôi khi process đã chết trước đó, bỏ qua lỗi này
            }
        }
    }
}