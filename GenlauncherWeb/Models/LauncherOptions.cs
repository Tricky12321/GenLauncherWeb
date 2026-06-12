using System.Linq;
using GenLauncherWeb.Enums;
using GenLauncherWeb.Services;

namespace GenLauncherWeb.Models;

public class LauncherOptions
{
    public InstallMethod InstallMethod { get; set; }
    public string SteamPath { get; set; }
    public GameType SelectedGame { get; set; }

    public static LauncherOptions DefaultSettings()
    {
        var options = new LauncherOptions
        {
            InstallMethod = SymLinkService.IsSymlinksSupported() ? InstallMethod.SymLink : InstallMethod.CopyFiles,
            SteamPath = TryDetectSteamPath()
        };
        options.FixOptions();
        return options;
    }

    /// <summary>
    /// Repairs options that have become invalid: falls back from symlinks when no
    /// longer supported, re-detects an empty Steam path, and picks a selected game
    /// when none is set (preferring an installed one, Zero Hour first).
    /// </summary>
    public void FixOptions()
    {
        if (InstallMethod == InstallMethod.SymLink && !SymLinkService.IsSymlinksSupported())
        {
            InstallMethod = InstallMethod.CopyFiles;
        }

        if (string.IsNullOrEmpty(SteamPath))
        {
            SteamPath = TryDetectSteamPath();
        }

        if (SelectedGame != GameType.Gen && SelectedGame != GameType.ZH)
        {
            var detected = SteamService.DetectInstalledGames(SteamPath);
            SelectedGame = detected.Contains(GameType.ZH) ? GameType.ZH
                : detected.Contains(GameType.Gen) ? GameType.Gen
                : GameType.ZH;
        }
    }

    /// <summary>
    /// Steam path detection must not make options unreadable on machines without
    /// Steam; an empty path is reported through the checkSteamPath endpoint instead.
    /// </summary>
    private static string TryDetectSteamPath()
    {
        try
        {
            return SteamService.GetSteamInstallPath();
        }
        catch
        {
            return "";
        }
    }
}

public enum InstallMethod
{
    CopyFiles,
    SymLink
}
