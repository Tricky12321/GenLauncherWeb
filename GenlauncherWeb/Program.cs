using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using ElectronNET.API;

namespace GenLauncherWeb;

public static class Program
{
    public static void Main(string[] args)
    {
        CreateWebHostBuilder(args).Build().Run();
    }

    private static IWebHostBuilder CreateWebHostBuilder(string[] args)
    {
        return WebHost
            .CreateDefaultBuilder(args)
            .UseElectron(args)
            .UseStartup<Startup>();
    }
}