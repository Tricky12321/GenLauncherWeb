using System.Collections.Generic;
using GenLauncherWeb.Enums;

namespace GenLauncherWeb.Models;

public class ReposModsData
{
    /// <summary>Which game this repo data belongs to (not part of the YAML; set by the backend).</summary>
    public GameType Game { get; set; }

    public string LauncherVersion { get; set; }
    public string DownloadLink { get; set; }
    public string VulkanReposData { get; set; }
    public List<ModAddonsAndPatches> modDatas = new List<ModAddonsAndPatches>();

    public List<string> globalAddonsData = new List<string>();
    public List<string> originalGameAddons = new List<string>();
    public List<string> originalGamePatches = new List<string>();

    public List<AdvertisingData> AdvData = new List<AdvertisingData>();
}

public class ModAddonsAndPatches
{
    public int ModId { get; set; }
    public string ModName { get; set; }
    public string ModLink { get; set; }
    public List<string> ModPatches { get; set; }

    public List<string> ModAddons { get; set; }

    /// <summary>Set by the backend for the browse list; not part of the repo YAML.</summary>
    public bool Added { get; set; }

    /// <summary>Set by the backend for the browse list; not part of the repo YAML.</summary>
    public bool Downloaded { get; set; }

    /// <summary>Set by the backend for the browse list; not part of the repo YAML.</summary>
    public bool Installed { get; set; }

    public ModAddonsAndPatches()
    {
        ModPatches = new List<string>();
        ModAddons = new List<string>();
    }
}

public class AdvertisingData
{
    public string ModName { get; set; }
    public string ModLink { get; set; }
    public List<string> ImagesData { get; set; }

    public AdvertisingData()
    {
        ImagesData = new List<string>();
    }
}