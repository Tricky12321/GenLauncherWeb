using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace GenLauncherWeb.Models;

public class Mod
{
    public bool Installed { get; set; }
    public bool Installing { get; set; }
    public bool Downloading { get; set; }
    public bool Downloaded { get; set; }
    public string DownloadedVersion { get; set; }
    public ModAddonsAndPatches ModInfo { get; set; }

    public ModData ModData
    {
        get
        {
            if (ModInfo == null)
            {
                return null;
            }

            if (_modData == null)
            {
                _modData = ModInfo.DownloadModData();
            }

            return _modData;
        }
    }

    public string CleanedModName => ModInfo.ModName.CleanString();
    public string ModDir { get; set; }
    public ulong TotalSize { get; set; }
    public List<string> DownloadedFiles { get; set; }

    [NotMapped] private ModData _modData { get; set; }
    
}