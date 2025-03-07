using System.Threading.Tasks;
using GenLauncherWeb.Models.RequestObjects;
using GenLauncherWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace GenLauncherWeb.Controllers;

[Route("api/[controller]")]
public class GeneralController : ControllerBase
{
    public readonly SteamService _steamService;
    private readonly RepoService _repoService;
    private readonly ModService _modService;
    private readonly OptionsService _optionsService;

    public GeneralController(SteamService steamService, RepoService repoService, ModService modService, OptionsService optionsService)
    {
        _steamService = steamService;
        _repoService = repoService;
        _modService = modService;
        _optionsService = optionsService;
    }

    [HttpGet("modlist")]
    public IActionResult GetModList()
    {
        var modList = _modService.GetUnAddedMods();
        return Ok(modList);
    }

    [HttpGet("paths")]
    public IActionResult GetPaths()
    {
        var optionsSteamPath = _optionsService.GetOptions().SteamPath;
        var applicationConfigFolder = OptionsService.GetApplicationDataFile();
        SteamService.GetGeneralsInstallDir(optionsSteamPath);
        return Ok(new
        {
            SteamInstallPath = optionsSteamPath,
            ConfigPath = applicationConfigFolder
        });
    }


    [HttpGet("addedMods")]
    public IActionResult GetAddedMods()
    {
        var addedMods = _modService.GetAddedMods();
        return Ok(addedMods);
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
        var modInstallInfo = _modService.GetModDownloadProgress(modName);
        return Ok(modInstallInfo);
    }

    [HttpPost("uninstallMod")]
    public IActionResult UninstallMod([FromBody] ModRequest modRequest)
    {
        _modService.UninstallMod(modRequest.ModName);
        return Ok();
    }
    /*
    [HttpPost("selectMod")]
    public IActionResult SelectMod([FromBody] ModRequest modRequest)
    {
        _modService.SelectMod(modRequest.ModName);
        return Ok();
    }
    */

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
        var status = _modService.GetInstallationStatus();
        return Ok(status);
    }

    [HttpGet("installGenTool")]
    public IActionResult InstallGenTool()
    {
        _modService.EnsureGenToolInstalled();
        return Ok();
    }

    [HttpGet("checkSteamPath")]
    public IActionResult CheckSteamPath()
    {
        var steamPath = SteamService.GetSteamInstallPath();
        return Ok(new { SteamPath = steamPath });
    }
}