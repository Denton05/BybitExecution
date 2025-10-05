using System.Text.Json;

namespace BybitExecution.Models;

public sealed record ExecutionEvent(string ExecId, string Symbol, string Side, decimal Price, string Qty, string LocalTime, string UtcIso)
{
    public static bool TryParse(JsonElement fill, out ExecutionEvent ev)
    {
        ev = default!;
        
        var execId = fill.TryGetProperty("execId", out var e) ? e.GetString() : null;
        if(string.IsNullOrEmpty(execId))
        {
            return false;
        }

        var symbol = fill.TryGetProperty("symbol", out var s) ? s.GetString() ?? "?" : "?";
        var side = fill.TryGetProperty("side", out var sd) ? sd.GetString() ?? "?" : "?";
        var qty = fill.TryGetProperty("execQty", out var q) ? q.GetString() ?? "?" : "?";

        var price = 0m;
        if (fill.TryGetProperty("execPrice", out var p))
        {
            decimal.TryParse(p.GetString(), out price);
        }

        var tsRaw = fill.TryGetProperty("execTime", out var t) ? t.GetString() : null;
        string local = "?", utc = "?";
        if (long.TryParse(tsRaw, out var ms))
        {
            var ts = DateTimeOffset.FromUnixTimeMilliseconds(ms);
            local = ts.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            utc = ts.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }

        ev = new ExecutionEvent(execId, symbol, side, price, qty, local, utc);
        return true;
    }
}
