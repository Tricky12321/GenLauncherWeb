using System.Linq;
using System.Threading.Tasks;
using ElectronNET.API;
using ElectronNET.API.Entities;
using GenLauncherWeb.Models.RequestObjects;
using GenLauncherWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace GenLauncherWeb.Controllers;

[Route("api/[controller]")]
public class GeneralController : ControllerBase
{
    private readonly ModService _modService;
    private readonly OptionsService _optionsService;

    public GeneralController(ModService modService, OptionsService optionsService)
    {
        _modService = modService;
        _optionsService = optionsService;
    }

    [HttpGet("modlist")]
    public IActionResult GetModList()
    {
        return Ok(_modService.GetBrowseMods());
    }

    [HttpGet("paths")]
    public IActionResult GetPaths()
    {
        return Ok(new
        {
            SteamInstallPath = _optionsService.GetOptions().SteamPath,
            ConfigPath = OptionsService.GetApplicationDataFile()
        });
    }

    [HttpGet("detectedGames")]
    public IActionResult GetDetectedGames()
    {
        var options = _optionsService.GetOptions();
        return Ok(new
        {
            DetectedGames = SteamService.DetectInstalledGames(options.SteamPath),
            SelectedGame = options.SelectedGame
        });
    }

    [HttpGet("addedMods")]
    public IActionResult GetAddedMods()
    {
        return Ok(_modService.GetAddedMods());
    }

    [HttpPost("removeMod")]
    public IActionResult RemoveMod([FromBody] ModRequest modRequest)
    {
        _modService.RemoveModFromModList(modRequest.ModName);
        return Ok();
    }

    [HttpPost("addMod")]
    public IActionResult AddMod([FromBody] ModRequest modRequest)
    {
        _modService.AddModToModList(modRequest.ModName);
        return Ok();
    }

    [HttpPost("downloadMod")]
    public async Task<IActionResult> DownloadMod([FromBody] ModRequest modRequest)
    {
        await _modService.DownloadMod(modRequest.ModName);
        return Ok();
    }

    [HttpGet("getModDownloadProgress/{modName}")]
    public IActionResult GetModDownloadProgress(string modName)
    {
        return Ok(_modService.GetModDownloadProgress(modName));
    }

    [HttpPost("uninstallMod")]
    public IActionResult UninstallMod([FromBody] ModRequest modRequest)
    {
        _modService.UninstallMod(modRequest.ModName);
        return Ok();
    }

    [HttpPost("deleteMod")]
    public IActionResult DeleteMod([FromBody] ModRequest modRequest)
    {
        _modService.DeleteMod(modRequest.ModName);
        return Ok();
    }

    [HttpPost("installMod")]
    public IActionResult InstallMod([FromBody] ModRequest modRequest)
    {
        _modService.InstallMod(modRequest.ModName);
        return Ok();
    }

    [HttpGet("GetInstallationStatus")]
    public IActionResult GetInstallationStatus()
    {
        return Ok(_modService.GetInstallationStatus());
    }

    [HttpGet("installGenTool")]
    public IActionResult InstallGenTool()
    {
        _modService.EnsureGenToolInstalled();
        return Ok();
    }

    [HttpGet("browseSteamFolder")]
    public async Task<IActionResult> BrowseSteamFolder()
    {
        if (!HybridSupport.IsElectronActive)
        {
            return Ok(new { available = false, path = (string)null });
        }

        var win = Electron.WindowManager.BrowserWindows.FirstOrDefault();
        if (win == null)
        {
            return Ok(new { available = false, path = (string)null });
        }

        var dialogOptions = new OpenDialogOptions
        {
            Title = "Select steamapps/common folder",
            Properties = new[] { OpenDialogProperty.openDirectory }
        };

        var paths = await Electron.Dialog.ShowOpenDialogAsync(win, dialogOptions);
        var selected = paths?.FirstOrDefault();
        return Ok(new { available = true, path = selected ?? (string)null });
    }

    [HttpGet("checkSteamPath")]
    public IActionResult CheckSteamPath()
    {
        var configuredPath = _optionsService.GetOptions().SteamPath;
        var steamPath = string.IsNullOrEmpty(configuredPath) ? SteamService.GetSteamInstallPath() : configuredPath;
        return Ok(new { SteamPath = steamPath });
    }

}
