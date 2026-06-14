using System.Threading.Tasks;
using GenLauncherWeb.Models.RequestObjects;
using GenLauncherWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace GenLauncherWeb.Controllers;

[Route("api/[controller]")]
public class PatchController : ControllerBase
{
    private readonly PatchService _patchService;

    public PatchController(PatchService patchService)
    {
        _patchService = patchService;
    }

    [HttpGet]
    public IActionResult GetPatches()
    {
        return Ok(_patchService.GetPatches());
    }

    [HttpPost("download")]
    public async Task<IActionResult> DownloadPatch([FromBody] PatchUrlRequest request)
    {
        await _patchService.DownloadPatch(request.PatchUrl);
        return Ok();
    }

    [HttpGet("progress")]
    public IActionResult GetProgress([FromQuery] string patchUrl)
    {
        return Ok(_patchService.GetDownloadProgress(patchUrl));
    }

    [HttpPost("install")]
    public IActionResult InstallPatch([FromBody] PatchUrlRequest request)
    {
        _patchService.InstallPatch(request.PatchUrl);
        return Ok();
    }

    [HttpPost("uninstall")]
    public IActionResult UninstallPatch([FromBody] PatchUrlRequest request)
    {
        _patchService.UninstallPatch(request.PatchUrl);
        return Ok();
    }

    [HttpPost("delete")]
    public IActionResult DeletePatch([FromBody] PatchUrlRequest request)
    {
        _patchService.DeletePatch(request.PatchUrl);
        return Ok();
    }
}
