using System.IO;
using GenLauncherWeb.Models;
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
    
    public void SetOptions(LauncherOptions launcherOptions)
    {
        _launcherOptions = launcherOptions;
        UpdateOptionsFile();
    }
    
    public LauncherOptions GetOptions()
    {
        ReadModListFile();
        return _launcherOptions;
    }

    private void UpdateOptionsFile()
    {
        var filePath = _steamService.GetGameInstallDir();
        var jsonFile = Path.Combine(filePath, "genlauncher_modlist.json");
        var json = JsonConvert.SerializeObject(_launcherOptions);
        File.WriteAllText(jsonFile, json);
    }
}