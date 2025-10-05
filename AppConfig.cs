using System.Text.Json;

namespace BybitExecution;

sealed class AppConfig
{
    public BybitSettings Bybit { get; init; } = new();

    public static BybitSettings Load(string path = "appsettings.json")
    {
        if(!File.Exists(path))
        {
            throw new FileNotFoundException($"Config file '{path}' not found.");
        }

        var json = File.ReadAllText(path);
        var cfg = JsonSerializer.Deserialize<AppConfig>(json)
                  ?? throw new InvalidOperationException("Failed to parse config.");

        var envKey = Environment.GetEnvironmentVariable("BYBIT_API_KEY");
        var envSecret = Environment.GetEnvironmentVariable("BYBIT_API_SECRET");
        var envWs = Environment.GetEnvironmentVariable("BYBIT_WS_URL");

        var settings = cfg.Bybit with
        {
            ApiKey = string.IsNullOrWhiteSpace(envKey) ? cfg.Bybit.ApiKey : envKey,
            ApiSecret = string.IsNullOrWhiteSpace(envSecret) ? cfg.Bybit.ApiSecret : envSecret,
            WsUrl = string.IsNullOrWhiteSpace(envWs) ? cfg.Bybit.WsUrl : envWs
        };

        if(string.IsNullOrWhiteSpace(settings.ApiKey) || string.IsNullOrWhiteSpace(settings.ApiSecret))
        {
            throw new InvalidOperationException("Bybit ApiKey/ApiSecret are missing. Fill appsettings.json or set env variables.");
        }

        return settings;
    }
}
