// HourglassLibrary/Interfaces/IWebSocketCommunicator.cs
using System.Threading.Tasks;

namespace HourglassLibrary.Interfaces
{
    public interface IWebSocketCommunicator
    {
        Task SendCloseTabCommand(string domain);
        // Add more methods if needed (e.g., Start, Stop, SendMessage)
    }
}