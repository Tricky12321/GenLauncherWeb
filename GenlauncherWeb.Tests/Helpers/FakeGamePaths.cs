using GenLauncherWeb.Enums;
using GenLauncherWeb.Services;

namespace GenlauncherWeb.Tests.Helpers;

/// <summary>
/// Test double for <see cref="IGamePaths"/> that points every location at a temp
/// directory, so install/backup/restore logic can run without a real Steam install.
/// </summary>
public sealed class FakeGamePaths : IGamePaths
{
    private readonly string _root;

    public FakeGamePaths(string root)
    {
        _root = root;
        Directory.CreateDirectory(ModStorageDir);
        Directory.CreateDirectory(GameDir(GameType.Gen));
        Directory.CreateDirectory(GameDir(GameType.ZH));
    }

    public string ModStorageDir => Path.Combine(_root, "ModStorage");

    public string GameDir(GameType game) => Path.Combine(_root, "Game_" + game);

    public string BackupRoot(GameType game) => Path.Combine(ModStorageDir, "OriginalGameFiles", game.ToString());
}
