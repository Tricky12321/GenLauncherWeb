using GenLauncherWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace GenLauncherWeb.Controllers;

[Route("api/[controller]")]
public class GeneralController : ControllerBase
{
    public readonly SteamService _steamService;
    private readonly RepoService _repoService;

    public GeneralController(SteamService steamService, RepoService repoService)
    {
        _steamService = steamService;
        _repoService = repoService;
    }

    [HttpGet("modlist")]
    public IActionResult GetModList()
    {
        var modList = _repoService.GetRepoData();
        return Ok(modList);
    }

    [HttpGet("steamInstallPath")]
    public IActionResult GetSteamInstallPath()
    {
        var steamInstallPath = _steamService.GetSteamInstallPath();
        _steamService.GetGeneralInstallDir(steamInstallPath);
        return Ok(new { SteamInstallPath = steamInstallPath });
    }
}