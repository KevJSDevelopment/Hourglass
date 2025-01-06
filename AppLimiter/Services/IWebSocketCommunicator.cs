public interface IWebSocketCommunicator
{
    Task SendCloseTabCommand(string domain);
}