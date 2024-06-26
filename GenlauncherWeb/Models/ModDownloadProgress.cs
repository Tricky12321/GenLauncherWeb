using System;
using System.Collections.Generic;

namespace GenLauncherWeb.Models;

public class ModDownloadProgress
{
    public ulong TotalDownloadSize { get; set; }
    public ulong DownloadedSize { get; set; }
    public List<string> FileList { get; set; }
    public List<string> DownloadedFiles { get; set; }
    public bool Downloaded { get; set; }

    public decimal Percentage
    {
        get
        {
            if (TotalDownloadSize == 0)
            {
                return 0;
            }

            return Math.Floor((decimal)DownloadedSize / TotalDownloadSize * 100);
        }
    }
}