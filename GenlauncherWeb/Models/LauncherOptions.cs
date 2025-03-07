using GenLauncherWeb.Services;

namespace GenLauncherWeb.Models;

public class LauncherOptions
{
    public InstallMethod InstallMethod { get; set; }

    public static LauncherOptions DefaultSettings()
    {
        return new LauncherOptions()
        {
            InstallMethod = SymLinkService.IsSymlinksSupported() ? InstallMethod.SymLink : InstallMethod.CopyFiles,
            SteamPath = SteamService.GetSteamInstallPath()
        };
    }
    
    public void FixOptions()
    {
        if (InstallMethod == InstallMethod.SymLink && !SymLinkService.IsSymlinksSupported())
        {
            InstallMethod = InstallMethod.CopyFiles;
        }
        if (string.IsNullOrEmpty(SteamPath))
        {
            SteamPath = SteamService.GetSteamInstallPath();
        }
    }
    public string SteamPath { get; set; }
}

public enum InstallMethod
{
    CopyFiles,
    SymLink
}