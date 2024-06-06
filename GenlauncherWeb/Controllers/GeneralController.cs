using GenLauncherWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace GenLauncherWeb.Controllers;

[Route("api/[controller]")]
public class GeneralController : ControllerBase
{
    public readonly SteamService _steamService;

    public GeneralController(SteamService steamService)
    {
        _steamService = steamService;
    }
    [HttpGet("steamInstallPath")]
    public IActionResult GetSteamInstallPath()
    {
        var steamInstallPath = _steamService.GetSteamGamePath();
        _steamService.GetGeneralInstallDir(steamInstallPath);
        return Ok(new {SteamInstallPath = steamInstallPath});
    }
}