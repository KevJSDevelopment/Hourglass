// HourglassLibrary/Services/WebsiteTracker.cs
using HourglassLibrary.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;

namespace HourglassLibrary.Services
{
    public class WebsiteTracker : IWebsiteTracker
    {
        private readonly ILogger<WebsiteTracker> _logger;
        private readonly Dictionary<string, HashSet<string>> _activeUrls = new();
        private readonly ReaderWriterLockSlim _lock = new();

        public WebsiteTracker(ILogger<WebsiteTracker> logger)
        {
            _logger = logger;
        }

        private string GetDomainFromUrl(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            {
                return uri.Host.ToLower().Replace("www.", "");
            }
            return url.ToLower();
        }

        public void UpdateUrls(IEnumerable<string> urls)
        {
            _lock.EnterWriteLock();
            try
            {
                _logger.LogInformation("Updating active URLs. Current count: {Count}", _activeUrls.Count);
                _activeUrls.Clear();
                foreach (var url in urls)
                {
                    var domain = GetDomainFromUrl(url);
                    _logger.LogDebug("Processing URL {Url} with domain {Domain}", url, domain);
                    if (!_activeUrls.ContainsKey(domain))
                        _activeUrls[domain] = new HashSet<string>();

                    _activeUrls[domain].Add(url);
                }
                _logger.LogInformation("Updated active URLs. New count: {Count}", _activeUrls.Count);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool IsDomainActive(string domainOrUrl)
        {
            _lock.EnterReadLock();
            try
            {
                var domain = GetDomainFromUrl(domainOrUrl);
                var isActive = _activeUrls.ContainsKey(domain);
                _logger.LogDebug("Checking if domain {Domain} is active: {IsActive}", domain, isActive);
                return isActive;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
}