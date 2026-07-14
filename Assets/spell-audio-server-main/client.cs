using System.Net.Sockets;
using System.Text;
using System;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;


public class AudioClient : MonoBehaviour
{
    private Process serverProcess;

    private byte[] buffer = new byte[1024];

    private TcpClient client;
    private NetworkStream stream => client.GetStream();


    public event Action<string> OnRecognition;

    public static AudioClient Instance;

    [SerializeField]
    public int deviceId;

    [SerializeField]
    public string language = "en-us";

    private bool isReading = false;

    private async Task StartServer() {
        string serverPath = Path.Combine(
            Application.streamingAssetsPath,
            "AudioServer",
            "server"
        );

        serverProcess = new Process();

        serverProcess.StartInfo = new ProcessStartInfo {
            FileName = serverPath,
            Arguments = "--device " + deviceId + " --language " + language,
            WorkingDirectory = Path.GetDirectoryName(serverPath),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        serverProcess.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Debug.Log("[Server] " + e.Data);
        };

        serverProcess.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Debug.LogError("[Server] " + e.Data);
        };

        serverProcess.Start();
        serverProcess.BeginOutputReadLine();
        serverProcess.BeginErrorReadLine();

        // Give the server a moment to start.
        await Task.Delay(3000);
    }

    private async Task ConnectToServer() {
        client = new TcpClient();

        while (true) {
            try {
                await client.ConnectAsync("localhost", 65432);
                break;
            } catch (SocketException) {
                await Task.Delay(100);
            }
        }
    }

    private async void Awake() {
        if (Instance == null)
            Instance = this;
        else Destroy(this);

        await StartServer();
        await ConnectToServer();
    }

    private void Update()
    {
        ReadAudio();
    }

    async private void ReadAudio()
    {
        if (client == null || stream == null || isReading) return;
        
        isReading = true;

        int bytesRead = await stream.ReadAsync(buffer);
        if (bytesRead == 0) return;

        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        Debug.Log($"Received: {message}");

        OnRecognition?.Invoke(message);
        
        isReading = false;
    }
}