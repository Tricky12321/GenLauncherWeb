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

    private void ReadModListFile()
    {
        var filePath = _steamService.GetGameInstallDir();
        var jsonFile = Path.Combine(filePath, LauncherOptionsFile);
        if (File.Exists(jsonFile))
        {
            _launcherOptions = JsonConvert.DeserializeObject<LauncherOptions>(File.ReadAllText(jsonFile));
        }
        else
        {
            _launcherOptions = LauncherOptions.DefaultSettings();
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
        var filePath = _steamService.GetGameInstallDir();
        var jsonFile = Path.Combine(filePath, LauncherOptionsFile);
        var json = JsonConvert.SerializeObject(_launcherOptions);
        File.WriteAllText(jsonFile, json);
    }

    

    public LauncherOptions ResetOptions()
    {
        _launcherOptions = LauncherOptions.DefaultSettings();
        UpdateOptionsFile();
        return _launcherOptions;
    }
}