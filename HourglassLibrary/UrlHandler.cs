using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HourglassLibrary
{
    public class UrlHandler
    {
        public static string GetDomainFromUrl(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            {
                return uri.Host.ToLower().Replace("www.", "");
            }
            return url.ToLower();
        }
    }
}
