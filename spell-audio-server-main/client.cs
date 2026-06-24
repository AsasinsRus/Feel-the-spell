using System.Net.Sockets;
using System.Text;

using var client = new TcpClient("172.26.0.1", 65432);
using var stream = client.GetStream();

Console.WriteLine("Connected. Waiting for messages...");

byte[] buffer = new byte[1024];
while (true)
{
    int bytesRead = await stream.ReadAsync(buffer);
    if (bytesRead == 0) break;

    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
    Console.WriteLine($"Received: {message}");
}
