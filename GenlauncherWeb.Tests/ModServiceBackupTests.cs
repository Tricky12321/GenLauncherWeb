using FluentAssertions;
using GenLauncherWeb.Enums;
using GenLauncherWeb.Services;
using GenlauncherWeb.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace GenlauncherWeb.Tests;

/// <summary>
/// Exercises <see cref="ModService"/>'s backup/restore of original game files against a
/// temp directory via a fake <see cref="IGamePaths"/> — no Steam install required. This
/// is the filesystem seam that makes the service layer testable.
/// </summary>
[TestFixture]
public class ModServiceBackupTests
{
    private string _root = null!;
    private FakeGamePaths _paths = null!;
    private ModService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), $"GL_Svc_{Guid.NewGuid():N}"[..24]);
        Directory.CreateDirectory(_root);
        _paths = new FakeGamePaths(_root);

        var config = new ConfigurationBuilder().Build();
        _service = new ModService(
            new RepoService(config),
            new SteamService(),
            new S3StorageService(config),
            new OptionsService(),
            _paths,
            httpClientFactory: null!,
            NullLogger<ModService>.Instance,
            config);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_root, recursive: true);

    [Test]
    public void BackupOriginalGameFile_MovesFileIntoPerGameBackupFolder()
    {
        var game = GameType.ZH;
        var original = Path.Combine(_paths.GameDir(game), "INI.big");
        File.WriteAllText(original, "original game data");

        _service.BackupOriginalGameFile(game, "INI.big");

        File.Exists(original).Should().BeFalse("the original is moved out of the game folder");
        var backup = Path.Combine(_paths.BackupRoot(game), "INI.big");
        File.Exists(backup).Should().BeTrue();
        File.ReadAllText(backup).Should().Be("original game data");
    }

    [Test]
    public void BackupOriginalGameFile_MissingFile_IsNoOp()
    {
        var act = () => _service.BackupOriginalGameFile(GameType.Gen, "does_not_exist.big");
        act.Should().NotThrow();
    }

    [Test]
    public void RestoreOriginalGameFile_MovesBackupBackIntoGameFolder()
    {
        var game = GameType.Gen;
        var original = Path.Combine(_paths.GameDir(game), "data", "Maps.big");
        Directory.CreateDirectory(Path.GetDirectoryName(original)!);
        File.WriteAllText(original, "v1");

        _service.BackupOriginalGameFile(game, Path.Combine("data", "Maps.big"));
        File.Exists(original).Should().BeFalse();

        var restored = _service.RestoreOriginalGameFile(game, Path.Combine("data", "Maps.big"));

        restored.Should().BeTrue();
        File.Exists(original).Should().BeTrue();
        File.ReadAllText(original).Should().Be("v1");
    }

    [Test]
    public void RestoreOriginalGameFile_NoBackup_ReturnsFalse()
    {
        _service.RestoreOriginalGameFile(GameType.Gen, "never_backed_up.big").Should().BeFalse();
    }

    [Test]
    public void BackupThenRestore_RoundTripsContentAndLocation()
    {
        var game = GameType.ZH;
        var rel = "Generals.exe";
        var gameFile = Path.Combine(_paths.GameDir(game), rel);
        File.WriteAllText(gameFile, "pristine exe");

        _service.BackupOriginalGameFile(game, rel);
        // Simulate a mod dropping its own file in place
        File.WriteAllText(gameFile, "modded exe");

        _service.RestoreOriginalGameFile(game, rel);

        File.ReadAllText(gameFile).Should().Be("pristine exe");
    }
}
