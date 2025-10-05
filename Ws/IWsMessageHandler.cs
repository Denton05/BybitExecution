namespace BybitExecution.Ws;

public interface IWsMessageHandler
{
    void Handle(string json);
}
