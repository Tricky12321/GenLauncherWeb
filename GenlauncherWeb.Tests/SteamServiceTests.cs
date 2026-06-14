using FluentAssertions;
using GenLauncherWeb.Enums;
using GenLauncherWeb.Services;
using NUnit.Framework;

namespace GenlauncherWeb.Tests;

/// <summary>
/// Tests Steam folder detection and game-directory resolution using a fake Steam
/// installation built in a temp directory. No real Steam installation is needed.
/// </summary>
[TestFixture]
public class SteamServiceTests
{
    private string _steamRoot = null!;
    private string _commonDir = null!;

    [SetUp]
    public void SetUp()
    {
        _steamRoot = Path.Combine(Path.GetTempPath(), $"GL_Steam_{Guid.NewGuid():N}"[..26]);
        _commonDir = Path.Combine(_steamRoot, "steamapps", "common");
        Directory.CreateDirectory(_commonDir);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_steamRoot, recursive: true);

    // -------------------------------------------------------------- helpers

    /// <summary>Creates a minimal appmanifest_&lt;appId&gt;.acf next to the common folder.</summary>
    private void WriteManifest(GameType game, string installDirName)
    {
        var steamApps = Path.GetDirectoryName(_commonDir)!;
        var manifestPath = Path.Combine(steamApps, $"appmanifest_{SteamService.GetAppId(game)}.acf");
        File.WriteAllText(manifestPath, $$"""
            "AppState"
            {
                "appid"      "{{SteamService.GetAppId(game)}}"
                "name"       "Test Game"
                "installdir" "{{installDirName}}"
            }
            """);
    }

    /// <summary>Creates an actual game directory (fallback detection uses folder presence).</summary>
    private void CreateGameDir(string folderName) =>
        Directory.CreateDirectory(Path.Combine(_commonDir, folderName));

    // -------------------------------------------------------------- GetGameInstallDir

    [Test]
    public void GetGameInstallDir_ManifestPresent_ReturnsManifestInstallDir()
    {
        WriteManifest(GameType.Gen, "CnC Generals Custom");

        var result = SteamService.GetGameInstallDir(GameType.Gen, _commonDir);

        result.Should().Be(Path.Combine(_commonDir, "CnC Generals Custom"));
    }

    [Test]
    public void GetGameInstallDir_NoManifest_FallsBackToDefaultFolderName()
    {
        // No manifest file — should return the hard-coded default name
        var result = SteamService.GetGameInstallDir(GameType.Gen, _commonDir);

        result.Should().Contain("Command and Conquer Generals");
        result.Should().StartWith(_commonDir);
    }

    [Test]
    public void GetGameInstallDir_ZeroHour_ReturnsZeroHourPath()
    {
        WriteManifest(GameType.ZH, "CnCGeneralsZeroHour");

        var result = SteamService.GetGameInstallDir(GameType.ZH, _commonDir);

        result.Should().Be(Path.Combine(_commonDir, "CnCGeneralsZeroHour"));
    }

    // -------------------------------------------------------------- DetectInstalledGames

    [Test]
    public void DetectInstalledGames_ManifestPresent_DetectsGame()
    {
        WriteManifest(GameType.Gen, "Generals");

        var detected = SteamService.DetectInstalledGames(_commonDir);

        detected.Should().Contain(GameType.Gen);
    }

    [Test]
    public void DetectInstalledGames_GameDirectoryPresent_DetectsGame()
    {
        // Detection can also use the folder name from the manifest → game dir
        WriteManifest(GameType.ZH, "ZeroHour");
        CreateGameDir("ZeroHour");

        var detected = SteamService.DetectInstalledGames(_commonDir);

        detected.Should().Contain(GameType.ZH);
    }

    [Test]
    public void DetectInstalledGames_EmptyCommonFolder_ReturnsEmpty()
    {
        // The method also falls back to real Steam auto-detection; skip if games are installed on this machine.
        var detected = SteamService.DetectInstalledGames(_commonDir);
        if (detected.Count > 0)
            Assert.Ignore("Game(s) auto-detected via real Steam installation — skipping isolation check.");

        detected.Should().BeEmpty();
    }

    [Test]
    public void DetectInstalledGames_BothGamesPresent_ReturnsBoth()
    {
        WriteManifest(GameType.Gen, "Generals");
        WriteManifest(GameType.ZH, "ZeroHour");

        var detected = SteamService.DetectInstalledGames(_commonDir);

        detected.Should().Contain(GameType.Gen).And.Contain(GameType.ZH);
    }

    // -------------------------------------------------------------- GetModDir

    [Test]
    public void GetModDir_ReturnsSubdirectoryOfCommonFolder()
    {
        var modDir = SteamService.GetModDir(_commonDir);

        modDir.Should().Be(Path.Combine(_commonDir, SteamService.ModDirName));
    }

    [Test]
    public void GetModDir_FolderNameMatchesConstant()
    {
        SteamService.ModDirName.Should().NotBeNullOrWhiteSpace();
    }

    // -------------------------------------------------------------- GetAppId

    [Test]
    public void GetAppId_Generals_ReturnsKnownId()
    {
        SteamService.GetAppId(GameType.Gen).Should().Be(SteamService.GeneralsAppId);
    }

    [Test]
    public void GetAppId_ZeroHour_ReturnsKnownId()
    {
        SteamService.GetAppId(GameType.ZH).Should().Be(SteamService.ZeroHourAppId);
    }

    [Test]
    public void GetAppId_BothGames_ReturnDifferentIds()
    {
        SteamService.GetAppId(GameType.Gen).Should().NotBe(SteamService.GetAppId(GameType.ZH));
    }
}
