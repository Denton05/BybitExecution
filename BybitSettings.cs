namespace BybitExecution;

public sealed record BybitSettings
{
    public string ApiKey { get; init; } = string.Empty;
    public string ApiSecret { get; init; } = string.Empty;
    public string WsUrl { get; init; } = string.Empty;
}
