using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ElectronNET.API;
using ElectronNET.API.Entities;
using GenLauncherWeb.Middleware;
using GenLauncherWeb.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SpaServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GenLauncherWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Custom services. They hold process-wide state (mod lists, caches),
            // so they are singletons.
            services.AddSingleton<SteamService>();
            services.AddSingleton<RepoService>();
            services.AddSingleton<S3StorageService>();
            services.AddSingleton<IGamePaths, GamePaths>();
            services.AddSingleton<ModService>();
            services.AddSingleton<OptionsService>();
            services.AddSingleton<PatchService>();

            // Pooled HTTP clients (avoids socket exhaustion from new HttpClient() per call).
            // The "download" client allows the long transfers mods/patches need.
            services.AddHttpClient();
            services.AddHttpClient("download", client => client.Timeout = TimeSpan.FromHours(2));

            services.AddElectron();
            services.AddMvc(options => options.EnableEndpointRouting = false);
            services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    //Set datetime to local og hosting machine
                    options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Local;
                    //Optimize formatting
                    options.SerializerSettings.Formatting = Formatting.None;
                    //Ignore json self ReferenceLoopHandling
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    //Don't json null values
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                });
            // In production, the Angular files will be served from this directory
            // (the Angular 19 application builder emits into dist/browser)
            services.AddSpaStaticFiles(configuration => { configuration.RootPath = "ClientApp/dist/browser"; });
            services.AddControllersWithViews();
        }

        private Process _spaDevServerProcess;

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime,
            ILogger<Startup> logger)
        {
            if (env.EnvironmentName == "Development")
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseMiddleware<ErrorHandlingMiddleware>();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                // To learn more about options for serving an Angular SPA from ASP.NET Core,
                // see https://go.microsoft.com/fwlink/?linkid=864501
                //  Check if port 4200 is in use

                spa.Options.SourcePath = "ClientApp";
                if ((env.IsDevelopment() || Environment.GetEnvironmentVariable("USE_DEV_SITE") == "true" || Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"))
                {
                    if (IsPortInUse(4200))
                    {
                        spa.UseProxyToSpaDevelopmentServer("http://localhost:" + 4200);
                    }
                    else
                    {
                        string angularProjectPath = spa.Options.SourcePath;
                        int port = GetFreePort();
                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = "ng", Arguments = $"serve --port {port}", WorkingDirectory = angularProjectPath, UseShellExecute = true,
                        };

                        _spaDevServerProcess = Process.Start(psi);
                        // Make sure the spawned `ng serve` is torn down with the app
                        lifetime.ApplicationStopping.Register(StopSpaDevServer);
                        spa.UseProxyToSpaDevelopmentServer("http://localhost:" + port);
                    }
                }
                else
                {
                    // The Angular 19 application builder emits into dist/browser
                    var root = Path.Combine(env.ContentRootPath, "ClientApp", "dist", "browser");

                    if (Directory.Exists(root))
                    {
                        spa.Options.DefaultPageStaticFileOptions = new StaticFileOptions
                        {
                            FileProvider = new PhysicalFileProvider(root), OnPrepareResponse = context => { context.Context.Response.Headers.Append("Cache-Control", "max-age=3000, must-revalidate"); }
                        };
                    }
                }
            });

            if (Environment.GetEnvironmentVariable("NO_ELECTRON") != "true")
            {
                // The window can only be pointed at the real server address once the
                // server has started listening
                var addressFeature = app.ServerFeatures.Get<IServerAddressesFeature>();
                lifetime.ApplicationStarted.Register(() =>
                {
                    var address = addressFeature?.Addresses.FirstOrDefault(a => a.StartsWith("http://"))
                                  ?? addressFeature?.Addresses.FirstOrDefault()
                                  ?? "http://localhost:8002";
                    address = address.Replace("0.0.0.0", "localhost").Replace("[::]", "localhost").Replace("*", "localhost");
                    // Fire-and-forget, but exceptions are observed inside the task rather
                    // than crashing the process as an unobserved async void would.
                    _ = ElectronBootstrapAsync(address, logger);
                });
            }
        }

        private static int GetFreePort()
        {
            int port = new Random().Next(45001, 65535);
            do
            {
                // Check if the port is available
                using (var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, port))
                {
                    try
                    {
                        listener.Start();
                        listener.Stop();

                        break; // Port is available, exit the loop
                    }
                    catch
                    {
                        // If port is in use, try another one
                        port = new Random().Next(45001, 65535);
                    }
                }
            } while (port == 0);

            return port;
        }

        private static bool IsPortInUse(int port)
        {
            return System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .Any(p => p.Port == port);
        }

        private void StopSpaDevServer()
        {
            try
            {
                if (_spaDevServerProcess is { HasExited: false })
                {
                    _spaDevServerProcess.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Best effort: the dev server may already be gone.
            }
        }

        private static async Task ElectronBootstrapAsync(string appUrl, ILogger logger)
        {
            try
            {
                BrowserWindowOptions options = new BrowserWindowOptions
                {
                    Show = false,
                    Width = 1200,
                    Height = 800,
                    Fullscreen = false,
                };

                BrowserWindow mainWindow = await Electron.WindowManager.CreateWindowAsync(options, appUrl);
                mainWindow.OnReadyToShow += () => { mainWindow.Show(); };
                mainWindow.SetTitle("GenLauncher");
                MenuItem[] menu = new MenuItem[]
                {
                    new MenuItem
                    {
                        Label = "File",
                        Submenu = new MenuItem[]
                        {
                            new MenuItem
                            {
                                Label = "Exit",
                                Click = () => { Electron.App.Exit(); }
                            }
                        }
                    },
                    new MenuItem
                    {
                        Label = "Info",
                        Click = async () => { await Electron.Dialog.ShowMessageBoxAsync("Welcome to App"); }
                    }
                };

                Electron.Menu.SetApplicationMenu(menu);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to bootstrap the Electron window");
            }
        }
    }
}
