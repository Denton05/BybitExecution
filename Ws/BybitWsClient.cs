using System.Buffers;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BybitExecution.Ws;

public sealed class BybitWsClient
{
    private readonly string _wsUrl;
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private readonly IWsMessageHandler _handler;
    private readonly CancellationToken _ct;

    public BybitWsClient(BybitSettings cfg, IWsMessageHandler handler, CancellationToken ct)
    {
        _wsUrl = cfg.WsUrl;
        _apiKey = cfg.ApiKey;
        _apiSecret = cfg.ApiSecret;
        _handler = handler;
        _ct = ct;
    }

    public async Task RunAsync(string[] topics)
    {
        while (!_ct.IsCancellationRequested)
        {
            using var ws = new ClientWebSocket();
            try
            {
                Console.WriteLine("[WS] Connecting...");
                await ws.ConnectAsync(new Uri(_wsUrl), _ct);

                await AuthenticateAsync(ws);
                await SubscribeAsync(ws, topics);

                var receiveTask = ReceiveLoopAsync(ws);
                var pingTask = PingLoopAsync(ws, TimeSpan.FromSeconds(20));

                await Task.WhenAny(receiveTask, pingTask);

                Console.WriteLine("[WS] Closing...");
                try
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
                }
                catch { }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WS] Error: {ex.Message}");
            }

            if (!_ct.IsCancellationRequested)
            {
                Console.WriteLine("[WS] Reconnecting in 2s...");
                await Task.Delay(TimeSpan.FromSeconds(2), _ct);
            }
        }
    }

    private async Task AuthenticateAsync(ClientWebSocket ws)
    {
        var expires = DateTimeOffset.UtcNow.AddSeconds(60).ToUnixTimeMilliseconds();

        var payload = "GET/realtime" + expires;
        var signature = ComputeHmacSha256Hex(_apiSecret, payload);

        var auth = new
        {
            op = "auth",
            args = new object[] { _apiKey, expires, signature }
        };

        await SendAsync(ws, auth);
        Console.WriteLine("[AUTH] Sent");

        var message = await ReceiveSingleAsync(ws);
        Console.WriteLine($"[AUTH Response]: {message}");

        if (message.Contains("\"success\":false", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[AUTH] Warning: auth response not successful.");
        }
    }

    private async Task SubscribeAsync(ClientWebSocket ws, string[] topics)
    {
        var subscribe = new
        {
            op = "subscribe",
            args = topics
        };

        await SendAsync(ws, subscribe);
        Console.WriteLine("[SUBSCRIBE] " + string.Join(", ", topics));
    }

    private async Task PingLoopAsync(ClientWebSocket ws, TimeSpan interval)
    {
        while (ws.State == WebSocketState.Open && !_ct.IsCancellationRequested)
        {
            await Task.Delay(interval, _ct);
            var ping = new
            {
                op = "ping",
                req_id = "ping-" + DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            await SendAsync(ws, ping);
        }
    }

	private async Task ReceiveLoopAsync(ClientWebSocket ws)
	{
		var buffer = new byte[64 * 1024];
		var collector = new ArrayBufferWriter<byte>();

		while(!_ct.IsCancellationRequested && ws.State == WebSocketState.Open)
		{
			collector.Clear();
			WebSocketReceiveResult result;
			do
			{
				result = await ws.ReceiveAsync(buffer, _ct);

				if(result.MessageType == WebSocketMessageType.Close)
				{
					return;
				}

				collector.Write(buffer.AsSpan(0, result.Count));
			} while(!result.EndOfMessage);

			var json = Encoding.UTF8.GetString(collector.WrittenSpan);

            try
            {
                _handler.Handle(json);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[HANDLE] {ex.Message}");
            }
		}
	}

    private async Task SendAsync(ClientWebSocket ws, object payload, CancellationToken? ct = null)
    {
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = null });
        var bytes = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct ?? CancellationToken.None);
    }

	private async Task<string> ReceiveSingleAsync(ClientWebSocket ws)
	{
		var buffer = new byte[32 * 1024];
		var collector = new ArrayBufferWriter<byte>();

		while(true)
		{
			var result = await ws.ReceiveAsync(buffer, _ct);
			if(result.MessageType == WebSocketMessageType.Close)
			{
				return "{\"close\":true}";
			}

			collector.Write(buffer.AsSpan(0, result.Count));

			if(result.EndOfMessage)
			{
				break;
			}
		}

		return Encoding.UTF8.GetString(collector.WrittenSpan);
	}

	private static string ComputeHmacSha256Hex(string secret, string payload)
	{
		using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
		var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
		var sb = new StringBuilder(hash.Length * 2);
		foreach(var b in hash)
		{
			sb.Append($"{b:x2}");
		}

		return sb.ToString();
	}
}
