using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using GenLauncherWeb.Enums;
using Microsoft.Extensions.DependencyInjection;

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

    public string GetGeneralsInstallDir(string steamInstallPath)
    {
        string generalInstallDir = Path.Combine(steamInstallPath, "Command and Conquer Generals");
        return generalInstallDir;
    }
    public string GetGeneralsInstallDir()
    {
        var steamInstallPath = GetSteamInstallPath();
        return GetGeneralsInstallDir(steamInstallPath);
    }
    
    public string GetZeroHourInstallDir(string steamInstallPath)
    {
        string generalInstallDir = Path.Combine(steamInstallPath, "Command & Conquer Generals - Zero Hour");
        return generalInstallDir;
    }
    public string GetZeroHourInstallDir()
    {
        var steamInstallPath = GetSteamInstallPath();
        return GetZeroHourInstallDir(steamInstallPath);
    }

    public string GetGameInstallDir()
    {
        return GetGame() == GameType.ZH ? GetZeroHourInstallDir() : GetGeneralsInstallDir();
    }
    public string GetCommonFolder()
    {
        return GetSteamInstallPath();
    }
    
    public static GameType GetGame()
    {
        // TODO: Implement way to detect game, maybe some way to select which game to mod for
        return GameType.ZH;
    }

    public void CreateModsFolder()
    {
        var installDir = GetGeneralsInstallDir();
        var modsDir = Path.Combine(installDir, "_mods");
        if (!Directory.Exists(modsDir))
        {
            Directory.CreateDirectory(modsDir);
        }
    }

    public bool CheckGameRunning()
    {
        // Implement a way to detect if a process called Generals.exe is running
        string processName = "generals.exe";
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Process.GetProcesses()
                .Any(p => string.Equals(p.ProcessName, processName.Replace(".exe", ""), StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "ps",
                Arguments = "aux",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            using (var reader = process.StandardOutput)
            {
                string output = reader.ReadToEnd();
                return output.ToLower().Contains(processName);
            }
        }
    }

    public string GetModDir()
    {
        return Path.Combine(GetCommonFolder(), "GenLauncherMods");
    }

    public string GetSteamUserdataDir()
    {
        return Path.Combine(GetSteamInstallPath(), "../userdata");
    }
    
}