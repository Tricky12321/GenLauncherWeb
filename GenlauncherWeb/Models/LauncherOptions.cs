namespace GenLauncherWeb.Models;

public class LauncherOptions
{
    public InstallMethod InstallMethod { get; set; }

    public static LauncherOptions DefaultSettings()
    {
        return new LauncherOptions()
        {
            InstallMethod = InstallMethod.SymLink
        };
    }
}

public enum InstallMethod
{
    MoveFiles,
    CopyFiles,
    SymLink
}