using System;
using System.IO;
using GenLauncherWeb.Enums;
using GenLauncherWeb.Models;
using Newtonsoft.Json;

namespace GenLauncherWeb.Services;

public class OptionsService
{
    private LauncherOptions _launcherOptions;
    private readonly object _lock = new();
    private const string LauncherOptionsFile = "genlauncher_options.json";

    public static string GetApplicationDataFolder()
    {
        string appDataFolder;

        if (OperatingSystem.IsWindows())
        {
            appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
        else if (OperatingSystem.IsMacOS())
        {
            appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support");
        }
        else if (OperatingSystem.IsLinux())
        {
            appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported platform");
        }

        appDataFolder = Path.Combine(appDataFolder, "genlauncher");
        if (!Directory.Exists(appDataFolder))
        {
            Directory.CreateDirectory(appDataFolder);
        }

        return appDataFolder;
    }

    public static string GetApplicationDataFile()
    {
        return Path.Combine(GetApplicationDataFolder(), LauncherOptionsFile);
    }

    public LauncherOptions GetOptions()
    {
        lock (_lock)
        {
            _launcherOptions ??= LoadOptions();
            return _launcherOptions;
        }
    }

    public LauncherOptions SetOptions(LauncherOptions launcherOptions)
    {
        lock (_lock)
        {
            launcherOptions.FixOptions();
            _launcherOptions = launcherOptions;
            SaveOptionsFile(_launcherOptions);
            return _launcherOptions;
        }
    }

    public LauncherOptions ResetOptions()
    {
        lock (_lock)
        {
            _launcherOptions = LauncherOptions.DefaultSettings();
            SaveOptionsFile(_launcherOptions);
            return _launcherOptions;
        }
    }

    private LauncherOptions LoadOptions()
    {
        MigrateLegacyOptionsFile();
        var optionsFile = GetApplicationDataFile();

        LauncherOptions options;
        if (File.Exists(optionsFile))
        {
            options = JsonConvert.DeserializeObject<LauncherOptions>(File.ReadAllText(optionsFile)) ?? LauncherOptions.DefaultSettings();
            options.FixOptions();
        }
        else
        {
            options = LauncherOptions.DefaultSettings();
        }

        SaveOptionsFile(options);
        return options;
    }

    private static void SaveOptionsFile(LauncherOptions options)
    {
        File.WriteAllText(GetApplicationDataFile(), JsonConvert.SerializeObject(options));
    }

    /// <summary>
    /// Early builds stored the options file inside the Zero Hour game folder; move it
    /// to the config folder when found. Must never break options loading on machines
    /// where Steam or the game is missing.
    /// </summary>
    private static void MigrateLegacyOptionsFile()
    {
        try
        {
            var legacyFile = Path.Combine(SteamService.GetGameInstallDir(GameType.ZH), LauncherOptionsFile);
            var optionsFile = GetApplicationDataFile();
            if (File.Exists(legacyFile) && !File.Exists(optionsFile))
            {
                File.Move(legacyFile, optionsFile);
            }
        }
        catch
        {
            // No Steam / no game folder: nothing to migrate
        }
    }
}
