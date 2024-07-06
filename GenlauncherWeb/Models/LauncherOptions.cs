using GenLauncherWeb.Services;

namespace GenLauncherWeb.Models;

public class LauncherOptions
{
    public InstallMethod InstallMethod { get; set; }

    public static LauncherOptions DefaultSettings()
    {
        return new LauncherOptions()
        {
            InstallMethod = SymLinkService.IsSymlinksSupported() ? InstallMethod.SymLink : InstallMethod.CopyFiles
        };
    }
}

public enum InstallMethod
{
    CopyFiles,
    SymLink
}