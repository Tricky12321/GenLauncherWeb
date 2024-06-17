namespace GenLauncherWeb.Models;

public class Mod
{
    public bool Selected { get; set; }
    public bool Installed { get; set; }
    public string InstalledVersion { get; set; }
    public ModAddonsAndPatches ModInfo { get; set; }
}