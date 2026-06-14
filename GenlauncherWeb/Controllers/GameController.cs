using System.Diagnostics;
using GenLauncherWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace GenLauncherWeb.Controllers;

[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly OptionsService _optionsService;

    public GameController(OptionsService optionsService)
    {
        _optionsService = optionsService;
    }

    [HttpGet("start")]
    public IActionResult StartGame()
    {
        var game = _optionsService.GetOptions().SelectedGame;
        var steamUrl = $"steam://rungameid/{SteamService.GetAppId(game)}";
        Process.Start(new ProcessStartInfo(steamUrl) { UseShellExecute = true });
        return Ok();
    }
}
