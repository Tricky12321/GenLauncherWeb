using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace GenLauncherWeb.Models;

public class GamePatch
{
    public string PatchUrl { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string UIImageSourceLink { get; set; }
    public bool Downloaded { get; set; }
    public bool Installed { get; set; }
    public string PatchDir { get; set; }
    public List<string> DownloadedFiles { get; set; }
    public ulong TotalSize { get; set; }
    public ModData PatchData { get; set; }

    [NotMapped] public bool Downloading { get; set; }
}

public class GamePatchDto
{
    public string PatchUrl { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string UIImageSourceLink { get; set; }
    public bool Downloaded { get; set; }
    public bool Installed { get; set; }
    public bool Downloading { get; set; }
    public ulong TotalSize { get; set; }
}
