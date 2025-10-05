using BybitExecution.Ws;
using BybitExecution.Ws.Handlers;

namespace BybitExecution;

internal class Program
{
    public static async Task Main()
    {
        var cfg = AppConfig.Load();
        Console.WriteLine($"[CFG] WS URL: {cfg.WsUrl}");

        using var cts = new CancellationTokenSource();
		Console.CancelKeyPress += (_, e) =>
		{
		    e.Cancel = true;
            cts.Cancel();
		};

        var topics = new[] { "execution.fast.linear" };
        var handler = new ExecutionHandler();
        var client = new BybitWsClient(cfg, handler, cts.Token);

        await client.RunAsync(topics);
    }
}