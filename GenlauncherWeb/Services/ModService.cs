using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GenLauncherWeb.Models;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Newtonsoft.Json;

namespace GenLauncherWeb.Services;

public class ModService
{
    private readonly RepoService _repoService;
    private static List<Mod> _addedModList;
    private readonly SteamService _steamService;
    private static object _lock = new object();

    public ModService(RepoService repoService, SteamService steamService)
    {
        _repoService = repoService;
        _steamService = steamService;
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
                InstalledVersion = "",
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
        // Ensure the mod is not installed before removing
        if (_addedModList.First(x => x.ModInfo.ModName == modName).Installed == false)
        {
            _addedModList = _addedModList.Where(x => x.ModInfo.ModName != modName).ToList();
            UpdateModListFile();
        }

        return;
    }

    public List<Mod> GetAddedMods()
    {
        return _addedModList;
    }

    public void InstallMod(string modName)
    {
        lock (_lock)
        {
            throw new NotImplementedException("This has not been implemented yet");
            var mod = _addedModList.First(x => x.ModInfo.ModName == modName);
            mod.Installed = true;
            UpdateModListFile();
        }
    }

    public void UninstallMod(string modName)
    {
        lock (_lock)
        {
            throw new NotImplementedException("This has not been implemented yet");
            var mod = _addedModList.First(x => x.ModInfo.ModName == modName);
            mod.Installed = false;
            UpdateModListFile();
        }
    }

    public void SelectMod(string modName)
    {
        lock (_lock)
        {
            // Set all mods to not selected
            _addedModList = _addedModList.Select(x =>
            {
                x.Selected = false;
                return x;
            }).ToList();

            _addedModList.First(x => x.ModInfo.ModName == modName).Selected = true;
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
}