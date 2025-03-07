// HourglassLibrary/Interfaces/IWebsiteTracker.cs
namespace HourglassLibrary.Interfaces
{
    public interface IWebsiteTracker
    {
        void UpdateUrls(IEnumerable<string> urls);
        bool IsDomainActive(string domainOrUrl);
    }
}