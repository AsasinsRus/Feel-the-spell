using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine;
using UnityEngine.Events;
using static Unity.VisualScripting.Icons;
using Debug = UnityEngine.Debug;

public class AudioClient : MonoBehaviour
{
    private const int HISTORY_LENGTH = 3;

    public static string serverPath = Path.Combine(
            Application.streamingAssetsPath,
            "AudioServer",
            "server"
        );

    private Process serverProcess;

    private byte[] buffer = new byte[1024];

    private TcpClient client;
    private NetworkStream stream => client.GetStream();

    private Queue<string> history = new();

    public UnityEvent<string> OnRecognition;

    public static AudioClient Instance;

    [SerializeField]
    public int deviceId;

    [SerializeField]
    private string deviceName;
    [SerializeField, Tooltip("Just copy device name into \"Device Name\" field (if it's empty go to the 3 dots at right top and click on \"Refresh Microphones\")")]
    private string[] availableDevices;

    [SerializeField]
    public string language = "en-us";

    private bool isReading = false;

    public string DeviceName => deviceName;

    public bool IsSwitchingDevice { get; private set; } = false;



#if UNITY_EDITOR
    [ContextMenu("Refresh Microphones")]
    public void RefreshMicrophones()
    {
        availableDevices = Microphone.devices;
    }

#endif

    private async Task StartServer() {
        deviceId = PythonMicFinder.FindDeviceIndex(serverPath, deviceName);

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

        Debug.Log(serverProcess.Id);

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

        try {
            int bytesRead = await stream.ReadAsync(buffer);
            if (bytesRead == 0) return;

            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Debug.Log($"Received: {message}");

            history.Enqueue(message);
            if(history.Count > HISTORY_LENGTH)
                history.Dequeue();

            OnRecognition?.Invoke(HistoryAsString);
        } finally {
            isReading = false;
        }
    }

    /// <summary>
    /// Switches the active microphone by killing the current server process
    /// and restarting it with the new device name. Safe to call at runtime.
    /// </summary>
    public async Task ChangeDeviceAsync(string newDeviceName) {
        if (string.IsNullOrEmpty(newDeviceName))
            return;

        if (newDeviceName == deviceName)
            return;

        if (IsSwitchingDevice) {
            Debug.LogWarning("AudioClient: device switch already in progress, ignoring request.");
            return;
        }

        IsSwitchingDevice = true;

        try {
            deviceName = newDeviceName;
            await RestartServerConnection();
        } finally {
            IsSwitchingDevice = false;
        }
    }

    private void CloseServer() {
        // Stop reading and tear down the current connection/process.
        try { client?.Close(); } catch (Exception e) { Debug.LogWarning(e); }
        client = null;

        if (serverProcess != null) {
            try {
                if (!serverProcess.HasExited) {
                    if (!serverProcess.WaitForExit(1000))
                        serverProcess.Kill();
                }
            } catch (Exception e) {
                Debug.LogWarning(e);
            } finally {
                serverProcess.Dispose();
                serverProcess = null;
            }
        }

    }

    private async Task RestartServerConnection() {
        CloseServer();

        history.Clear();

        await StartServer();
        await ConnectToServer();
    }

    private void OnDestroy() {
        CloseServer();
    }

    private string HistoryAsString => string.Join(" ", history);
}

public static class PythonMicFinder
{ 
    public static int FindDeviceIndex(string pythonExe, string micName)
    {
        var devices = GetDevicesRawList(pythonExe);

        foreach (var rawLine in devices)
        {
            var line = rawLine.Trim();
            
            if(!line.Contains(micName))
                continue;

            if (!line.Contains("in, 0 out") && !line.Contains("in,"))
                continue;

            Match match = Regex.Match(line, @"^[><\s]*?(\d+)");
            if(match.Success)
                return int.Parse(match.Groups[1].Value);
        }

        return -1;
    }

    public static string[] GetDevicesRawList(string pythonExe = null)
    {
        if (pythonExe == null) pythonExe = AudioClient.serverPath;

        var psi = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = "--list-devices",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if(!string.IsNullOrWhiteSpace(error))
            Debug.LogWarning(error);

        return output.Replace("\r", "").Split('\n');
    }
}