using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace GenLauncherWeb.Services;

public class SteamService
{
    public string GetSteamInstallPath()
    {
        string zeroHourGameId = "2732960";
        string platform = DetectPlatform();
        string steamPath = "";

        if (platform == "Windows")
        {
            steamPath = @"C:\Program Files (x86)\Steam"; // Default Steam path
        }
        else if (platform == "Mac")
        {
            steamPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Application Support", "Steam");
        }
        else if (platform == "Linux")
        {
            steamPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam", "steam");
            if (!Directory.Exists(steamPath))
            {
                steamPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "Steam");
            }
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported platform");
        }

        string libraryFoldersPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");

        if (!File.Exists(libraryFoldersPath))
        {
            throw new FileNotFoundException("libraryfolders.vdf not found.");
        }

        var libraryPaths = GetLibraryPaths(libraryFoldersPath);

        foreach (var libraryPath in libraryPaths)
        {
            string acfFilePath = Path.Combine(libraryPath, "steamapps", $"appmanifest_{zeroHourGameId}.acf");

            if (File.Exists(acfFilePath))
            {
                return Path.Combine(libraryPath, "steamapps", "common");
            }
        }

        throw new Exception("Game not found.");
    }
    
    private List<string> GetLibraryPaths(string libraryFoldersPath)
    {
        var libraryPaths = new List<string>();
        var regex = new Regex(@"\""path\""\s+\""(.*?)\""", RegexOptions.Compiled);

        string[] lines = File.ReadAllLines(libraryFoldersPath);

        foreach (var line in lines)
        {
            var match = regex.Match(line);

            if (match.Success)
            {
                libraryPaths.Add(match.Groups[1].Value.Replace("\\\\", "\\"));
            }
        }

        libraryPaths.Add(Path.GetDirectoryName(Path.GetDirectoryName(libraryFoldersPath))); // Add the main Steam path
        return libraryPaths;
    }
    
    private static string DetectPlatform()
    {
        PlatformID platform = Environment.OSVersion.Platform;
        switch (platform)
        {
            case PlatformID.Win32NT:
            case PlatformID.Win32Windows:
                return "Windows";
            case PlatformID.MacOSX:
                return "Mac";
            case PlatformID.Unix:
                return "Linux";
            default:
                throw new PlatformNotSupportedException("Unsupported platform");
        }
    }

    public string GetGeneralInstallDir(string steamInstallPath)
    {
        string generalInstallDir = Path.Combine(steamInstallPath, "");
        return generalInstallDir;
    }
    public string GetGeneralInstallDir()
    {
        var steamInstallPath = GetSteamInstallPath();
        string generalInstallDir = Path.Combine(steamInstallPath, "");
        return generalInstallDir;
    }

    public void CreateModsFolder()
    {
        var installDir = GetGeneralInstallDir();
        var modsDir = Path.Combine(installDir, "mods");
        if (!Directory.Exists(modsDir))
        {
            Directory.CreateDirectory(modsDir);
        }
    }
}