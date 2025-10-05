using System.Text.Json;
using BybitExecution.Models;

namespace BybitExecution.Ws.Handlers;

public sealed class ExecutionHandler : IWsMessageHandler
{
    public void Handle(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if(root.TryGetProperty("op", out _))
        {
            Console.WriteLine($"[CTRL] {json}");
            return;
        }

        if(!root.TryGetProperty("topic", out var topicEl))
        {
            Console.WriteLine($"[SYS] {json}");
            return;
        }

        var topic = topicEl.GetString() ?? string.Empty;
        if(!topic.StartsWith("execution", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[MSG] {json}");
            return;
        }

        if(!root.TryGetProperty("data", out var dataEl) || dataEl.ValueKind != JsonValueKind.Array)
        {
            Console.WriteLine($"[EXEC] {json}");
            return;
        }

        foreach(var fill in dataEl.EnumerateArray())
        {
            if(!ExecutionEvent.TryParse(fill, out var ev))
            {
                continue;
            }

            if(Dedup.IsDuplicate(ev.ExecId))
            {
                continue;
            }

            Console.WriteLine($"[{ev.LocalTime}] Execution ID: {ev.ExecId}, Symbol: {ev.Symbol}, Side: {ev.Side}, Price: {ev.Price:F2}, Qty: {ev.Qty}, Time: {ev.UtcIso}");
        }
    }
}