using System;
using System.IO;
using System.Runtime.InteropServices;
using GenLauncherWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace GenLauncherWeb.Services;

public class OptionsService
{
    private readonly SteamService _steamService;
    private LauncherOptions _launcherOptions;
    private const string LauncherOptionsFile = "genlauncher_options.json";


    public OptionsService(SteamService steamService)
    {
        _steamService = steamService;
    }

    private void ReadModListFile()
    {
        var filePath = _steamService.GetGameInstallDir();
        var jsonFile = Path.Combine(filePath, LauncherOptionsFile);
        if (File.Exists(jsonFile))
        {
            _launcherOptions = JsonConvert.DeserializeObject<LauncherOptions>(File.ReadAllText(jsonFile));
        }
        else
        {
            _launcherOptions = LauncherOptions.DefaultSettings();
            UpdateOptionsFile();
        }
    }

    public void SetOptions(LauncherOptions launcherOptions)
    {
        _launcherOptions = launcherOptions;
        UpdateOptionsFile();
    }

    public LauncherOptions GetOptions()
    {
        ReadModListFile();
        return _launcherOptions;
    }

    private void UpdateOptionsFile()
    {
        var filePath = _steamService.GetGameInstallDir();
        var jsonFile = Path.Combine(filePath, LauncherOptionsFile);
        var json = JsonConvert.SerializeObject(_launcherOptions);
        File.WriteAllText(jsonFile, json);
    }

    public static bool IsSymlinksSupported()
    {
        // Check the platform
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // On Windows, symbolic link creation requires either:
            // - Administrator privileges, or
            // - Developer mode enabled on Windows 10 and above
            string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string symlink = tempFile + "_symlink";
            try
            {
                // Attempt to create a symlink
                File.Create(tempFile).Dispose();
                CreateSymbolicLink(symlink, tempFile, 0);
                bool success = File.Exists(symlink);
                File.Delete(tempFile);
                if (success)
                {
                    File.Delete(symlink);
                }

                return success;
            }
            catch
            {
                return false;
            }
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // On Linux and macOS, check if we can create a symlink in a temporary directory
            string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string symlink = tempFile + "_symlink";
            try
            {
                // Attempt to create a symlink
                File.Create(tempFile).Dispose();
                UnixCreateSymbolicLink(symlink, tempFile);
                bool success = File.Exists(symlink);
                File.Delete(tempFile);
                if (success)
                {
                    File.Delete(symlink);
                }

                return success;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

    private static void UnixCreateSymbolicLink(string symlink, string target)
    {
        var info = new System.Diagnostics.ProcessStartInfo("ln", $"-s \"{target}\" \"{symlink}\"")
        {
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using (var process = System.Diagnostics.Process.Start(info))
        {
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new Exception("Failed to create symlink");
            }
        }
    }

    public LauncherOptions ResetOptions()
    {
        _launcherOptions = LauncherOptions.DefaultSettings();
        UpdateOptionsFile();
        return _launcherOptions;
    }
}