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

    public GeneralController(SteamService steamService, RepoService repoService, ModService modService)
    {
        _steamService = steamService;
        _repoService = repoService;
        _modService = modService;
    }

    [HttpGet("modlist")]
    public IActionResult GetModList()
    {
        var modList = _modService.GetUnAddedMods();
        return Ok(modList);
    }

    [HttpGet("steamInstallPath")]
    public IActionResult GetSteamInstallPath()
    {
        var steamInstallPath = _steamService.GetSteamInstallPath();
        _steamService.GetGeneralsInstallDir(steamInstallPath);
        return Ok(new { SteamInstallPath = steamInstallPath });
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
    
    [HttpPost("selectMod")]
    public IActionResult SelectMod([FromBody] ModRequest modRequest)
    {
        _modService.SelectMod(modRequest.ModName);
        return Ok();
    }
    
    [HttpPost("deleteMod")]
    public IActionResult DeleteMod([FromBody] ModRequest modRequest)
    {
        _modService.DeleteMod(modRequest.ModName);
        return Ok();
    }
}

