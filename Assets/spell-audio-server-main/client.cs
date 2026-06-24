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

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else Destroy(this);

        client = new TcpClient("127.0.0.1", 65432);
    }

    private void Update()
    {
        if(stream != null)
            ReadAudio();
    }

    async private void ReadAudio()
    {
        int bytesRead = await stream.ReadAsync(buffer);
        if (bytesRead == 0) return;

        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        Debug.Log($"Received: {message}");

        OnRecognition?.Invoke(message);
    }
}



