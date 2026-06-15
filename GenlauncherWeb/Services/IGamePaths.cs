using System.IO;
using GenLauncherWeb.Enums;

namespace GenLauncherWeb.Services;

/// <summary>
/// Resolves the on-disk locations the launcher works with. Abstracted behind an
/// interface so install/backup/restore logic can be exercised against a temp directory
/// in tests, without a real Steam installation.
/// </summary>
public interface IGamePaths
{
    /// <summary>Launcher-managed mod storage folder (created if missing).</summary>
    string ModStorageDir { get; }

    /// <summary>The Steam install directory of the given game.</summary>
    string GameDir(GameType game);

    /// <summary>Per-game backup root for displaced original game files.</summary>
    string BackupRoot(GameType game);
}

/// <summary>
/// Production <see cref="IGamePaths"/> backed by Steam discovery and the user's options.
/// </summary>
public class GamePaths : IGamePaths
{
    private readonly OptionsService _optionsService;

    public GamePaths(OptionsService optionsService)
    {
        _optionsService = optionsService;
    }

    private string SteamPathOverride => _optionsService.GetOptions().SteamPath;

    public string ModStorageDir
    {
        get
        {
            var dir = SteamService.GetModDir(SteamPathOverride);
            dir.CreateFolderIfItDoesNotExist();
            return dir;
        }
    }

    public string GameDir(GameType game) => SteamService.GetGameInstallDir(game, SteamPathOverride);

    public string BackupRoot(GameType game)
        => Path.Combine(ModStorageDir, StorageNames.OriginalGameFilesDir, game.ToString());
}
