using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MyClient.JSON_Converter;
using MyClient;
using VisualClient.Pages;

public class AtmClientService
{
    //private readonly string _serverIp = IPAddress.Loopback.ToString();
    private readonly string _serverIp = "18.156.42.200";
    private readonly int _port = 10000;
    private TcpClient _client;
    private NetworkStream _stream;
    private readonly JsonSerializerOptions _jsonOptions;

    public AtmClientService()
    {
        _client = new TcpClient();
        _jsonOptions = new JsonSerializerOptions();
        _jsonOptions.Converters.Add(new RequestBaseConverter());
    }

    public async Task<ServerResponse?> SendAsync(RequestBase request)
    {
        try
        {
            if (_client == null || !_client.Connected)
            {
                _client = new TcpClient();
                await _client.ConnectAsync("TestBankpmat", _port);
                _stream = _client.GetStream();
            }

            string requestJson = JsonSerializer.Serialize(request, _jsonOptions);
            byte[] requestBytes = Encoding.UTF8.GetBytes(requestJson);

            await _stream.WriteAsync(requestBytes);
            await _stream.FlushAsync();

            var buffer = new byte[4096];
            using var ms = new MemoryStream();
            int bytesRead;
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            do
            {
                bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                if (bytesRead == 0) break;
                ms.Write(buffer, 0, bytesRead);
            } while (bytesRead == buffer.Length);

            string responseJson = Encoding.UTF8.GetString(ms.ToArray());
            var response = JsonSerializer.Deserialize<ServerResponse>(responseJson);
            return response;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}