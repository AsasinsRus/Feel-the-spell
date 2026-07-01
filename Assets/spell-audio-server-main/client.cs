using System.Net.Sockets;
using System.Text;
using System;
using UnityEngine;


public class AudioClient : MonoBehaviour
{
    private byte[] buffer = new byte[1024];

    private TcpClient client;
    private NetworkStream stream => client.GetStream();


    public event Action<string> OnRecognition;

    public static AudioClient Instance;

    [SerializeField]
    private string ip = "localhost";

    private bool isReading = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else Destroy(this);

        client = new TcpClient(ip, 65432);
    }

    private void Update()
    {
        ReadAudio();
    }

    async private void ReadAudio()
    {
        if (stream == null || isReading) return;
        
        isReading = true;

        int bytesRead = await stream.ReadAsync(buffer);
        if (bytesRead == 0) return;

        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        Debug.Log($"Received: {message}");

        OnRecognition?.Invoke(message);
        
        isReading = false;
    }
}