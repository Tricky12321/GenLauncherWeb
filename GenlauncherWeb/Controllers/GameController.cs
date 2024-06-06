using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace GenLauncherWeb.Controllers;

[Route("api/[controller]")]
public class GameController : ControllerBase
{
    [HttpGet("start")]
    public IActionResult StartGame()
    {
        string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        string workdir = Path.Combine(homeDirectory, ".steam/steam/steamapps/common/Command & Conquer Generals - Zero Hour");
        string arguments = "steam://rungameid/2732960";
        // Construct the steam URL

        // Use Process.Start to run the URL
        Process.Start(new ProcessStartInfo(arguments) { UseShellExecute = true });

        return Ok();
    }
}