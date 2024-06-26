using System;
using ElectronNET.API;
using ElectronNET.API.Entities;
using GenLauncherWeb.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            // Custom Services
            services.AddScoped<SteamService>();
            services.AddScoped<RepoService>();
            services.AddScoped<S3StorageService>();
            services.AddScoped<FileService>();
            services.AddScoped<ModService>();
            services.AddScoped<OptionsService>();
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
            services.AddSpaStaticFiles(configuration => { configuration.RootPath = "ClientApp/dist"; });
            services.AddControllersWithViews();
        }

        private bool IsDevelopment()
        {
            return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.EnvironmentName == "Development")
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

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

                spa.Options.SourcePath = "ClientApp";

                if (env.EnvironmentName == "Development")
                {
                    spa.UseAngularCliServer(npmScript: "start");
                }
            });
            if (Environment.GetEnvironmentVariable("NO_ELECTRON") != "true")
            {
                ElectronBootstrap();
            }
        }

        public async void ElectronBootstrap()
        {
            BrowserWindowOptions options = new BrowserWindowOptions
            {
                Show = false,
                Width = 1200,
                Height = 800,
                Fullscreen = false,
            };

            BrowserWindow mainWindow = await Electron.WindowManager.CreateWindowAsync(options, "http://localhost:8002");
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
    }
}