using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ElectronNET.API.Entities;
using GenLauncherWeb.Models;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace GenLauncherWeb.Services;

public class ModService
{
    private readonly RepoService _repoService;
    private static List<Mod> _addedModList;
    private readonly SteamService _steamService;
    private readonly S3StorageService _s3StorageService;
    private static object _lock = new object();
    private static ConcurrentDictionary<string, ModDownloadProgress> _modInstallInfo = new ConcurrentDictionary<string, ModDownloadProgress>();
    private readonly OptionsService _optionsService;
    private const string ModListFile = "genlauncher_modlist.json";
    public const string BackupOriginalGameFilesDir = "OriginalGameFiles";
    public readonly string ModdedGameExeHash;
    public readonly string ModdedGameExeDownloadLink;
    public readonly string GenToolDllHash;
    public readonly string GentoolDownloadLink;

    public ModService(RepoService repoService, SteamService steamService, S3StorageService s3StorageService, OptionsService optionsService, IConfiguration configuration)
    {
        _repoService = repoService;
        _steamService = steamService;
        _s3StorageService = s3StorageService;
        _optionsService = optionsService;
        ModdedGameExeHash = configuration["Extra:ModdedGameExeHash"];
        ModdedGameExeDownloadLink = configuration["Extra:ModdedExeDownloadLink"];
        GenToolDllHash = configuration["Extra:GentoolDllHash"];
        GentoolDownloadLink = configuration["Extra:GentoolDownloadLink"];
        ReadModListFile();
    }

    public void AddModToModList(string modName)
    {
        lock (_lock)
        {
            var repoData = _repoService.GetRepoData();
            var mod = repoData.modDatas.First(x => String.Equals(x.ModName.Trim(), modName.Trim(), StringComparison.CurrentCultureIgnoreCase));
            // Check if the mod is already added
            if (_addedModList.Any(x => x.ModInfo.ModName == mod.ModName))
            {
                return;
            }

            _addedModList.Add(new Mod
            {
                Installed = false,
                Downloaded = false,
                DownloadedVersion = "",
                ModInfo = mod
            });
            UpdateModListFile();
        }
    }

    private void UpdateModListFile()
    {
        var filePath = _steamService.GetModDir();
        var jsonFile = Path.Combine(filePath, ModListFile);
        var json = JsonConvert.SerializeObject(_addedModList);
        File.WriteAllText(jsonFile, json);
    }

    private void ReadModListFile()
    {
        lock (_lock)
        {
            var filePath = _steamService.GetModDir();
            var jsonFile = Path.Combine(filePath, ModListFile);
            if (File.Exists(jsonFile))
            {
                _addedModList = JsonConvert.DeserializeObject<List<Mod>>(File.ReadAllText(jsonFile));
            }
            else
            {
                _addedModList = new List<Mod>();
                UpdateModListFile();
            }
        }
    }

    public void RemoveModFromModList(string modName)
    {
        lock (_lock)
        {
            // Ensure the mod is not installed before removing
            if (_addedModList.First(x => x.ModInfo.ModName == modName).Installed == false)
            {
                _addedModList = _addedModList.Where(x => x.ModInfo.ModName != modName).ToList();
                UpdateModListFile();
            }
        }

        return;
    }

    public List<Mod> GetAddedMods()
    {
        return _addedModList;
    }

    public async Task DownloadMod(string modName)
    {
        var actualMod = _addedModList.First(x => x.ModInfo.ModName.Trim().ToLower() == modName.Trim().ToLower());
        lock (_lock)
        {
            actualMod.Downloading = true;
            UpdateModListFile();
        }

        var installedFiles = new List<string>();
        var cleanedModName = actualMod.ModInfo.ModName.CleanString();
        ulong totalInstallSize = 0;
        if (actualMod.HasS3Storage())
        {
            var modDir = Path.Combine(_steamService.GetModDir(), cleanedModName);
            modDir.CreateFolderIfIsNotExist();
            var fileList = _s3StorageService.GetModFiles(actualMod);
            ulong totalSize = 0;
            fileList.ForEach(x => { totalSize += x.Size; });
            CreateBaseDownloadProgress(totalSize, fileList, cleanedModName);
            foreach (var file in fileList)
            {
                var filePath = Path.Combine(modDir, file.FileName);
                if (File.Exists(filePath))
                {
                    // Check the hash of the file
                    var fileHash = filePath.GetMd5HashOfFile();
                    if (fileHash == file.Hash)
                    {
                        _modInstallInfo[cleanedModName].DownloadedFiles.Add(file.FileName);
                        _modInstallInfo[cleanedModName].DownloadedSize += file.Size;
                        totalInstallSize += file.Size;
                        Console.WriteLine("Hash matched for file " + file.FileName + " of size " + file.Size + ". It is already installed.");
                        // Files match, no need to redownload
                        continue;
                    }

                    // If the hash does not match, we delete it and redownload
                    File.Delete(filePath);
                }

                Path.GetDirectoryName(filePath).CreateFolderIfIsNotExist();
                var fileBytes = await _s3StorageService.DownloadS3File(file.FileName, actualMod);
                await File.WriteAllBytesAsync(filePath, fileBytes);
                // Check the hash of the file
                var hashMatch = filePath.GetMd5HashOfFile() == file.Hash;
                if (hashMatch)
                {
                    Console.WriteLine("Hash matched for file " + file.FileName + " of size " + file.Size);
                    installedFiles.Add(file.FileName);
                    _modInstallInfo[cleanedModName].DownloadedFiles.Add(file.FileName);
                    totalInstallSize += file.Size;
                    _modInstallInfo[cleanedModName].DownloadedSize += file.Size;
                }
                else
                {
                    throw new Exception("Hash did not match, file failed to install " + file.FileName);
                }
            }

            _modInstallInfo[cleanedModName].Downloaded = true;
        }
        else
        {
            var modDir = Path.Combine(_steamService.GetModDir(), cleanedModName);
            modDir.CreateFolderIfIsNotExist();
            byte[] fileBytes;
            MediaTypeHeaderValue fileMimeType;
            using (var client = new HttpClient())
            {
                var link = actualMod.ModData.SimpleDownloadLink.ParseDownloadLink();
                var result = await client.GetAsync(link);
                fileBytes = await result.Content.ReadAsByteArrayAsync();
                if (result.IsSuccessStatusCode)
                {
                    Console.WriteLine("Download complete!");
                }

                fileMimeType = result.Content.Headers.ContentType;
            }

            var fileExtension = fileMimeType.ToString().GetFileExtensionFromMimeType();
            var fileName = Path.Combine(modDir, cleanedModName + fileExtension);
            File.WriteAllBytes(fileName, fileBytes);
            // Extract the file
            
            
            
            
            installedFiles.Add(cleanedModName);
            totalInstallSize = (ulong)fileBytes.Length;
            _modInstallInfo[cleanedModName].Downloaded = true;
            _modInstallInfo[cleanedModName].DownloadedFiles = installedFiles;
            _modInstallInfo[cleanedModName].DownloadedSize = totalInstallSize;
        }

        lock (_lock)
        {
            var mod = _addedModList.First(x => x.ModInfo.ModName.Trim().ToLower() == modName.Trim().ToLower());
            mod.TotalSize = totalInstallSize;
            mod.Downloaded = true;
            mod.DownloadedVersion = mod.ModData.Version;
            mod.DownloadedFiles = installedFiles;
            mod.ModDir = Path.Combine(_steamService.GetModDir(), cleanedModName);
            UpdateModListFile();
        }
    }

    private void CreateBaseDownloadProgress(ulong totalSize, List<ModificationFileInfo> fileList, string cleanedModName)
    {
        ModDownloadProgress downloadProgress = new ModDownloadProgress()
        {
            TotalDownloadSize = totalSize,
            FileList = fileList.Select(x => x.FileName).ToList(),
            DownloadedFiles = new List<string>(),
            DownloadedSize = 0,
            Downloaded = false
        };
        _modInstallInfo.TryAdd(cleanedModName, downloadProgress);
    }

    public void UninstallMod(string modName)
    {
        lock (_lock)
        {
            var mod = _addedModList.First(x => x.ModInfo.ModName == modName);
            if (mod.Installed)
            {
                var gameFolder = _steamService.GetGameInstallDir();
                foreach (var modFile in mod.DownloadedFiles)
                {
                    var gameFilePath = Path.Combine(gameFolder, modFile);
                    // Attempt to restore an original game file, if it exists
                    // If the file cannot be restored, which would indicate it is not a original game file, delete it
                    if (!RestoreOriginalGameFile(modFile) && File.Exists(gameFilePath))
                    {
                        File.Delete(gameFilePath);
                    }
                }

                mod.Installed = false;
                UpdateModListFile();
            }
        }
    }

    public void InstallMod(string modName)
    {
        lock (_lock)
        {
            EnsureModdedLauncherInstalled();
            var mod = _addedModList.First(x => x.ModInfo.ModName == modName);
            // There can only be one installed mod at a time
            // This is to ensure mod compatibility
            if (_addedModList.Any(x => x.Installed))
            {
                throw new Exception("There is already another mod installed. Please uninstall it first.");
            }

            var gameFolder = _steamService.GetGameInstallDir();
            var modDir = _steamService.GetModDir();
            var modFolder = mod.ModDir;
            foreach (var modFile in mod.DownloadedFiles)
            {
                var gameFilePath = Path.Combine(gameFolder, modFile);
                if (File.Exists(gameFilePath))
                {
                    BackupOriginalGameFile(gameFilePath);
                }

                var options = _optionsService.GetOptions();
                var sourceFile = Path.Combine(modFolder, modFile);
                switch (options.InstallMethod)
                {
                    case InstallMethod.CopyFiles:
                        File.Copy(sourceFile, gameFilePath, true);
                        break;
                    case InstallMethod.SymLink:
                        SymLinkService.CreateSymbolicLink(gameFilePath,sourceFile);
                        break;
                }
            }

            mod.Installed = true;
            UpdateModListFile();
            EnsureModdedLauncherInstalled();
        }
    }

    /// <summary>
    /// Will move original game files into a seperate folder, keeping the folder structure
    /// </summary>
    /// <param name="filename"></param>
    public void BackupOriginalGameFile(string filename)
    {
        var gameDir = _steamService.GetGameInstallDir();
        var modDir = _steamService.GetModDir();
        var backupFolder = Path.Combine(modDir, BackupOriginalGameFilesDir);
        backupFolder.CreateFolderIfIsNotExist();

        var gameFile = Path.Combine(gameDir, filename);
        var destination = Path.Combine(backupFolder, filename);

        if (File.Exists(gameFile))
        {
            Path.GetDirectoryName(destination).CreateFolderIfIsNotExist();
            File.Move(gameFile, destination, true);
        }
    }

    /// <summary>
    /// Will attempt to restore a original game file.
    /// Return true of the file is restored
    /// Return false if the file is not restore or does not exist
    /// </summary>
    /// <param name="filename">This should be the relative path from the game root folder</param>
    /// <returns></returns>
    public bool RestoreOriginalGameFile(string filename)
    {
        var gameDir = _steamService.GetGameInstallDir();
        var modDir = _steamService.GetModDir();
        var backupFolder = Path.Combine(modDir, BackupOriginalGameFilesDir);

        var currentGameFile = Path.Combine(gameDir, filename);
        var backupFile = Path.Combine(backupFolder, filename);
        if (File.Exists(backupFile))
        {
            File.Move(backupFile, currentGameFile, true);
            return true;
        }
        else
        {
            return false;
        }
    }

    public ReposModsData GetUnAddedMods()
    {
        var repoData = _repoService.GetRepoData();
        repoData.modDatas = repoData.modDatas
            .Where(x => !_addedModList.Any(y => y.ModInfo.ModName == x.ModName))
            .ToList();
        return repoData;
    }

    public ModDownloadProgress GetModDownloadProgress(string modName)
    {
        if (_modInstallInfo.ContainsKey(modName))
        {
            if (_modInstallInfo[modName].Downloaded)
            {
                _modInstallInfo.Remove(modName, out var modProgress);
                return modProgress;
            }

            return _modInstallInfo[modName];
        }
        else
        {
            return new ModDownloadProgress()
            {
                DownloadedFiles = null,
                FileList = null,
                DownloadedSize = 0,
                TotalDownloadSize = 0,
                Downloaded = false
            };
        }
    }

    public void DeleteMod(string modRequestModName)
    {
        lock (_lock)
        {
            var mod = _addedModList.First(x => x.ModInfo.ModName == modRequestModName);
            mod.Downloaded = false;
            mod.DownloadedVersion = null;
            mod.Installed = false;
            try
            {
                Directory.Delete(mod.ModDir, true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            mod.DownloadedFiles = null;
            mod.TotalSize = 0;
            mod.ModDir = null;
            UpdateModListFile();
        }
    }

    public bool CheckModdedLauncherInstalled()
    {
        var gamePath = _steamService.GetGameInstallDir();
        var gameExe = Path.Combine(gamePath, "Generals.exe");
        if (File.Exists(gameExe))
        {
            var md5Hash = gameExe.GetMd5HashOfFile();
            if (md5Hash == ModdedGameExeHash)
            {
                return true;
            }
        }

        return false;
    }

    public bool EnsureModdedLauncherInstalled()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (_steamService.CheckGameRunning())
            {
                throw new Exception("Failed to installed modded launcher, mods will not work without it. Please ensure that the game is not running");
            }
        }

        try
        {
            var gamePath = _steamService.GetGameInstallDir();
            var modDir = _steamService.GetModDir();
            var gameExe = Path.Combine(gamePath, "Generals.exe");
            if (File.Exists(gameExe))
            {
                var md5Hash = gameExe.GetMd5HashOfFile();
                if (md5Hash == ModdedGameExeHash)
                {
                    return true;
                }
                else
                {
                    BackupOriginalGameFile("Generals.exe");
                    DownloadModdedExe();
                    InstallModdedLauncher();
                    return true;
                }
            }
            else
            {
                DownloadModdedExe();
                InstallModdedLauncher();
                return true;
            }
        }
        catch (Exception e)
        {
            throw new Exception("Unknown error, failed to installed modded launcher, mods will not work without it.\n" + e.ToString());
        }

        return false;
    }

    private void InstallModdedLauncher()
    {
        var gamePath = _steamService.GetGameInstallDir();
        var gameExe = Path.Combine(gamePath, "Generals.exe");
        var modFolder = _steamService.GetModDir();
        var moddedExeFolder = Path.Combine(modFolder, "ModdedLauncher");
        moddedExeFolder.CreateFolderIfIsNotExist();
        var moddedExeFile = Path.Combine(moddedExeFolder, "modded.exe");
        if (File.Exists(moddedExeFile))
        {
            var options = _optionsService.GetOptions();
            switch (options.InstallMethod)
            {
                case InstallMethod.CopyFiles:
                    File.Copy(moddedExeFile, gameExe, false);
                    break;
                case InstallMethod.SymLink:
                    SymLinkService.CreateSymbolicLink(moddedExeFile, gameExe);
                    break;
            }
        }
    }

    private void DownloadModdedExe()
    {
        var modFolder = _steamService.GetModDir();
        var moddedExeFolder = Path.Combine(modFolder, "ModdedLauncher");
        moddedExeFolder.CreateFolderIfIsNotExist();
        var moddedExeFile = Path.Combine(moddedExeFolder, "modded.exe");
        if (Directory.Exists(moddedExeFolder) && File.Exists(moddedExeFile))
        {
            return;
        }

        // Now download the modded.exe file from GitHub
        using (var client = new HttpClient())
        {
            var result = client.GetAsync(ModdedGameExeDownloadLink).GetAwaiter().GetResult();
            var fileData = result.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            var filePath = Path.Combine(moddedExeFolder, "modded.exe");
            File.WriteAllBytes(filePath, fileData);
        }
    }

    public InstallationStatus GetInstallationStatus()
    {
        bool moddedLauncher;
        try
        {
            moddedLauncher = CheckModdedLauncherInstalled();
        }
        catch (Exception e)
        {
            moddedLauncher = false;
        }

        bool genTool;
        try
        {
            genTool = CheckGenToolInstalled();
        }
        catch (Exception e)
        {
            genTool = false;
        }

        return new InstallationStatus
        {
            ModdedLauncher = moddedLauncher,
            GenTool = genTool
        };
    }

    public bool CheckGenToolInstalled()
    {
        _steamService.GetGameInstallDir();
        var gentoolDll = Path.Combine(_steamService.GetGameInstallDir(), "d3d8.dll");
        if (!File.Exists(gentoolDll))
        {
            return false;
        }

        var currentDllHash = gentoolDll.GetMd5HashOfFile();
        if (currentDllHash == GenToolDllHash)
        {
            return true;
        }

        return false;
    }

    public bool EnsureGenToolInstalled()
    {
        _steamService.GetGameInstallDir();
        var originalDll = Path.Combine(_steamService.GetGameInstallDir(), "d3d8.dll");
        if (File.Exists(originalDll))
        {
            var currentDllHash = originalDll.GetMd5HashOfFile();
            if (CheckGenToolInstalled())
            {
                return true;
            }
        }

        BackupOriginalGameFile("d3d8.dll");
        DownloadGenTool();
        InstallGenTool();
        return true;
    }

    private void InstallGenTool()
    {
        var gameDir = _steamService.GetGameInstallDir();
        var modDir = _steamService.GetModDir();
        var genToolFolder = Path.Combine(modDir, "GenTool");
        var d3d8Dll = Extensions.FindFileRecursively(genToolFolder, "d3d8.dll");
        if (File.Exists(d3d8Dll))
        {
            var options = _optionsService.GetOptions();
            switch (options.InstallMethod)
            {
                case InstallMethod.CopyFiles:
                    File.Copy(d3d8Dll, Path.Combine(gameDir, "d3d8.dll"), false);
                    break;
                case InstallMethod.SymLink:
                    SymLinkService.CreateSymbolicLink(Path.Combine(gameDir, "d3d8.dll"), d3d8Dll);
                    break;
            }
        }
    }

    private void DownloadGenTool()
    {
        var modDir = _steamService.GetModDir();
        var genToolFolder = Path.Combine(modDir, "GenTool");
        genToolFolder.CreateFolderIfIsNotExist();
        var genToolFile = Path.Combine(genToolFolder, "d3d8.dll");
        if (Directory.Exists(genToolFolder) && File.Exists(genToolFile))
        {
            return;
        }

        // Now download the d3d8.dll file from GitHub
        using (var client = new HttpClient())
        {
            var result = client.GetAsync(GentoolDownloadLink).GetAwaiter().GetResult();
            var fileData = result.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            var filePath = Path.Combine(genToolFolder, "gentool.zip");
            File.WriteAllBytes(filePath, fileData);
            ZipFile.ExtractToDirectory(filePath, genToolFolder);
            File.Delete(filePath);
        }
    }


}