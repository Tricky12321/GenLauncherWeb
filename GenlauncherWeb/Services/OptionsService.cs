using System;
using System.IO;
using System.Runtime.InteropServices;
using GenLauncherWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace GenLauncherWeb.Services;

public class OptionsService
{
    private readonly SteamService _steamService;
    private LauncherOptions _launcherOptions;
    private const string LauncherOptionsFile = "genlauncher_options.json";


    public OptionsService(SteamService steamService)
    {
        _steamService = steamService;
    }
    
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
            throw new Exception("Unsupported platform");
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
    
    private void ReadModListFile()
    {
        PatchJsonFileLocation();
        var applicationDataJsonFile = GetApplicationDataFile();
        
        if (File.Exists(applicationDataJsonFile))
        {
            _launcherOptions = JsonConvert.DeserializeObject<LauncherOptions>(File.ReadAllText(applicationDataJsonFile));
            _launcherOptions.FixOptions();
            UpdateOptionsFile();
        }
        else
        {
            _launcherOptions = LauncherOptions.DefaultSettings();
            _launcherOptions.FixOptions();
            UpdateOptionsFile();
        }
    }

    public LauncherOptions SetOptions(LauncherOptions launcherOptions)
    {
        _launcherOptions = launcherOptions;
        UpdateOptionsFile();
        return _launcherOptions;
    }

    public LauncherOptions GetOptions()
    {
        ReadModListFile();
        return _launcherOptions;
    }

    private void UpdateOptionsFile()
    {
        PatchJsonFileLocation();
        var applicationDataJsonFile = Path.Combine(GetApplicationDataFolder(), LauncherOptionsFile);

        var json = JsonConvert.SerializeObject(_launcherOptions);
        File.WriteAllText(applicationDataJsonFile, json);
    }

    public void PatchJsonFileLocation()
    {
        var filePath = SteamService.GetGameInstallDir();
        var steamFolderJsonFile = Path.Combine(filePath, LauncherOptionsFile);
        
        var applicationDataJsonFile = Path.Combine(GetApplicationDataFolder(), LauncherOptionsFile);
        
        if (File.Exists(steamFolderJsonFile))
        {
            File.Move(steamFolderJsonFile, applicationDataJsonFile);
        }
    }

    

    public LauncherOptions ResetOptions()
    {
        _launcherOptions = LauncherOptions.DefaultSettings();
        UpdateOptionsFile();
        return _launcherOptions;
    }
}