using System.ComponentModel.DataAnnotations.Schema;

namespace GenLauncherWeb.Models;

public class Mod
{
    public bool Selected { get; set; }
    public bool Installed { get; set; }
    public string InstalledVersion { get; set; }
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

    [NotMapped]
    private ModData _modData { get; set; }
    

}