using System;
using System.Linq;

namespace GenLauncherWeb;

/// <summary>
/// Rewrites share links (Dropbox, OneDrive) into direct-download URLs.
/// </summary>
public static class DownloadLinkParser
{
    public static string ParseDownloadLink(this string link)
    {
        if (link.Contains("dropbox.com"))
        {
            return link.Replace("?dl=0", "?dl=1");
        }

        if (link.Contains("onedrive.live.com"))
        {
            if (link.Contains("embed"))
            {
                return link.Replace("embed", "download");
            }

            var linkParts = link.Replace("https://onedrive.live.com/?", string.Empty).Split('&').ToList();

            var cid = linkParts.Where(t => t.Contains("cid=")).Select(t => t.Replace("cid=", string.Empty)).FirstOrDefault();
            var authKey = linkParts.Where(t => t.Contains("authkey=")).Select(t => t.Replace("authkey=", string.Empty)).FirstOrDefault();
            var resid = linkParts.Where(t => t.Contains("id=") && !t.Contains("cid=")).Select(t => t.Replace("id=", string.Empty)).FirstOrDefault();

            return string.Format("https://onedrive.live.com/download?cid={0}&resid={1}&authkey={2}", cid, resid, authKey);
        }

        return link;
    }
}
