using GenLauncherWeb.Models;
using GenLauncherWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace GenLauncherWeb.Controllers;

[Route("api/[controller]")]
public class OptionsController : ControllerBase
{
    private readonly OptionsService _optionsService;

    public OptionsController(OptionsService optionsService)
    {
        _optionsService = optionsService;
    }

    [HttpGet]
    public IActionResult GetOptions()
    {
        return Ok(_optionsService.GetOptions());
    }

    [HttpPost]
    public IActionResult SetOptions([FromBody] LauncherOptions launcherOptions)
    {
        _optionsService.SetOptions(launcherOptions);
        return Ok();
    }

    [HttpGet("reset")]
    public IActionResult ResetOptions()
    {
        var options = _optionsService.ResetOptions();
        return Ok(options);
    }

    [HttpGet("isSymlinksSupported")]
    public IActionResult IsSymlinksSupported()
    {
        return Ok(new
        {
            SymlinkSupported = OptionsService.IsSymlinksSupported()
        });
    }
}