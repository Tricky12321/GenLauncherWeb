using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GenLauncherWeb.Models;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
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

    public ModService(RepoService repoService, SteamService steamService, S3StorageService s3StorageService)
    {
        _repoService = repoService;
        _steamService = steamService;
        _s3StorageService = s3StorageService;
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
        var filePath = _steamService.GetGameInstallDir();
        var jsonFile = Path.Combine(filePath, "genlauncher_modlist.json");
        var json = JsonConvert.SerializeObject(_addedModList);
        File.WriteAllText(jsonFile, json);
    }

    private void ReadModListFile()
    {
        lock (_lock)
        {
            var filePath = _steamService.GetGameInstallDir();
            var jsonFile = Path.Combine(filePath, "genlauncher_modlist.json");
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
            var modDir = Path.Combine(_steamService.GetGameInstallDir(), "mods", cleanedModName);
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
            using (var client = new HttpClient())
            {
                var link = actualMod.ModData.SimpleDownloadLink.ParseDownloadLink();
                var result = await client.GetAsync(link);
                if (result.IsSuccessStatusCode)
                {
                    Console.WriteLine("Download complete!");
                }
            }
        }

        lock (_lock)
        {
            var mod = _addedModList.First(x => x.ModInfo.ModName.Trim().ToLower() == modName.Trim().ToLower());
            mod.TotalSize = totalInstallSize;
            mod.Downloaded = true;
            mod.DownloadedVersion = mod.ModData.Version;
            mod.DownloadedFiles = installedFiles;
            mod.ModDir = Path.Combine(_steamService.GetGameInstallDir(), "mods", cleanedModName);
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
                mod.UninstallMod();
                UpdateModListFile();
            }
        }
    }

    public void InstallMod(string modName)
    {
        lock (_lock)
        {
            var mod = _addedModList.First(x => x.ModInfo.ModName == modName);
            // There can only be one installed mod at a time
            // This is to ensure mod compatibility
            if (_addedModList.Any(x => x.Installed))
            {
                throw new Exception("There is already another mod installed. Please uninstall it first.");
            }
            mod.Installed = true;
            
            foreach (var modFile in mod.DownloadedFiles)
            {
                
            }
            UpdateModListFile();
        }
    }

    /// <summary>
    /// Install  a mod file into the game dir
    /// If the original game file exists, move it to a backup folder, keep relative path saved
    /// </summary>
    /// <param name="filename">Should be relative path</param>
    /// <returns></returns>
    public string InstallFile(string filename, string modPath)
    {
        
        
        
        return "";
    }

    public void SelectMod(string modName)
    {
        lock (_lock)
        {
            // Set all mods to not selected
            _addedModList = _addedModList.Select(x =>
            {
                x.Installed = false;
                return x;
            }).ToList();

            _addedModList.First(x => x.ModInfo.ModName == modName).Installed = true;
            UpdateModListFile();
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
    
    
}