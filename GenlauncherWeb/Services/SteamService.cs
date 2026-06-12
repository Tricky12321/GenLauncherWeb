using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using GenLauncherWeb.Enums;

namespace GenLauncherWeb.Services;

public class SteamService
{
    // Steam app ids of the supported games (verified against the Steam store)
    public const string GeneralsAppId = "2229870";
    public const string ZeroHourAppId = "2732960";

    // Fallback game folder names, only used when the appmanifest cannot be read
    private const string GeneralsDefaultFolder = "Command and Conquer Generals";
    private const string ZeroHourDefaultFolder = "Command & Conquer Generals - Zero Hour";

    public const string ModDirName = "GenLauncherMods";

    private static readonly Regex LibraryPathRegex = new(@"""path""\s+""(.*?)""", RegexOptions.Compiled);
    private static readonly Regex InstallDirRegex = new(@"""installdir""\s+""(.*?)""", RegexOptions.Compiled);

    public static string GetAppId(GameType game)
    {
        return game == GameType.Gen ? GeneralsAppId : ZeroHourAppId;
    }

    /// <summary>
    /// Possible Steam root folders for the current platform. Only candidates that
    /// exist on disk are returned.
    /// </summary>
    private static List<string> GetSteamRootCandidates()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        List<string> candidates;
        if (OperatingSystem.IsWindows())
        {
            candidates = new List<string> { @"C:\Program Files (x86)\Steam" };
        }
        else if (OperatingSystem.IsMacOS())
        {
            candidates = new List<string> { Path.Combine(home, "Library", "Application Support", "Steam") };
        }
        else if (OperatingSystem.IsLinux())
        {
            candidates = new List<string>
            {
                Path.Combine(home, ".steam", "steam"),
                Path.Combine(home, ".local", "share", "Steam"),
                // Flatpak Steam
                Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", "data", "Steam")
            };
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported platform, please enter your steam path manually.");
        }

        return candidates.Where(Directory.Exists).ToList();
    }

    /// <summary>
    /// All Steam library roots (folders that contain a steamapps directory),
    /// resolved from every libraryfolders.vdf of every Steam root candidate.
    /// </summary>
    public static List<string> GetAllLibraryPaths()
    {
        var libraries = new List<string>();
        foreach (var root in GetSteamRootCandidates())
        {
            var vdf = Path.Combine(root, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(vdf))
            {
                continue;
            }

            foreach (var line in File.ReadAllLines(vdf))
            {
                var match = LibraryPathRegex.Match(line);
                if (match.Success)
                {
                    libraries.Add(match.Groups[1].Value.Replace("\\\\", "\\"));
                }
            }

            libraries.Add(root);
        }

        return libraries.Distinct().Where(Directory.Exists).ToList();
    }

    private static string GetManifestPath(string libraryPath, GameType game)
    {
        return Path.Combine(libraryPath, "steamapps", $"appmanifest_{GetAppId(game)}.acf");
    }

    /// <summary>
    /// Appmanifest path resolved from a steamapps/common folder (the manifest lives
    /// one level up from common).
    /// </summary>
    private static string GetManifestFromCommonFolder(string commonPath, GameType game)
    {
        var steamApps = Path.GetDirectoryName(commonPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return Path.Combine(steamApps ?? commonPath, $"appmanifest_{GetAppId(game)}.acf");
    }

    /// <summary>
    /// The steamapps/common folder of the library containing the given game, or null.
    /// </summary>
    public static string FindGameCommonFolder(GameType game)
    {
        foreach (var library in GetAllLibraryPaths())
        {
            if (File.Exists(GetManifestPath(library, game)))
            {
                return Path.Combine(library, "steamapps", "common");
            }
        }

        return null;
    }

    /// <summary>
    /// The steamapps/common folder of the library containing any supported game.
    /// Throws when no Steam install of either game is found.
    /// </summary>
    public static string GetSteamInstallPath()
    {
        var common = FindGameCommonFolder(GameType.ZH) ?? FindGameCommonFolder(GameType.Gen);
        if (common == null)
        {
            throw new FileNotFoundException("No Steam installation of Command & Conquer Generals or Zero Hour was found. Please set the Steam common folder manually in Options.");
        }

        return common;
    }

    /// <summary>
    /// Which of the supported games are installed. When a steamapps/common path is
    /// given (user override from Options) it is checked first; auto-detection over
    /// all Steam libraries is used in addition.
    /// </summary>
    public static List<GameType> DetectInstalledGames(string steamCommonPath = null)
    {
        var detected = new List<GameType>();
        foreach (var game in new[] { GameType.Gen, GameType.ZH })
        {
            if (!string.IsNullOrEmpty(steamCommonPath) &&
                (File.Exists(GetManifestFromCommonFolder(steamCommonPath, game)) || Directory.Exists(GetGameInstallDir(game, steamCommonPath))))
            {
                detected.Add(game);
                continue;
            }

            try
            {
                if (FindGameCommonFolder(game) != null)
                {
                    detected.Add(game);
                }
            }
            catch (PlatformNotSupportedException)
            {
                // No auto-detection on unknown platforms; the manual path check above still applies
            }
        }

        return detected;
    }

    /// <summary>
    /// Install directory of a game. The folder name is read from the game's
    /// appmanifest when possible, falling back to the known default folder names.
    /// </summary>
    public static string GetGameInstallDir(GameType game, string steamCommonPath = null)
    {
        var common = steamCommonPath;
        if (string.IsNullOrEmpty(common))
        {
            common = FindGameCommonFolder(game) ?? GetSteamInstallPath();
        }

        var manifest = GetManifestFromCommonFolder(common, game);
        if (File.Exists(manifest))
        {
            var match = InstallDirRegex.Match(File.ReadAllText(manifest));
            if (match.Success)
            {
                return Path.Combine(common, match.Groups[1].Value);
            }
        }

        return Path.Combine(common, game == GameType.Gen ? GeneralsDefaultFolder : ZeroHourDefaultFolder);
    }

    /// <summary>
    /// The launcher-managed mod storage folder, next to the game folders.
    /// </summary>
    public static string GetModDir(string steamCommonPath = null)
    {
        var common = string.IsNullOrEmpty(steamCommonPath) ? GetSteamInstallPath() : steamCommonPath;
        return Path.Combine(common, ModDirName);
    }

    /// <summary>
    /// Both games use generals.exe as their process name.
    /// </summary>
    public bool CheckGameRunning()
    {
        const string processName = "generals.exe";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Process.GetProcesses()
                .Any(p => string.Equals(p.ProcessName, processName.Replace(".exe", ""), StringComparison.OrdinalIgnoreCase));
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "ps",
            Arguments = "aux",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        using var reader = process.StandardOutput;
        var output = reader.ReadToEnd();
        return output.ToLower().Contains(processName);
    }
}
