using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using GenLauncherWeb.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Task = System.Threading.Tasks.Task;

namespace GenLauncherWeb;

public class StartupService : BackgroundService
{
    public IServiceProvider Services { get; }

    public IConfiguration Configuration { get; set; }

    private static bool _hasScreenshotBeenTakenThisMonth;

    public StartupService(IServiceProvider services, IConfiguration configuration)
    {
        Services = services;
        Configuration = configuration;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (var scope = Services.CreateScope())
        {
            var repoService = scope.ServiceProvider.GetRequiredService<RepoService>();
            var test = repoService.GetRepoData();
            Console.WriteLine("loaded");
        }
    }
}