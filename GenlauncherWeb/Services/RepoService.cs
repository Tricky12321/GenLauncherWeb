using System;
using System.Linq;
using System.Net.Http;
using GenLauncherWeb.Enums;
using GenLauncherWeb.Models;
using Microsoft.Extensions.Configuration;
using YamlDotNet.Serialization;

namespace GenLauncherWeb.Services;

public class RepoService
{
    protected readonly string RepoUrl;
    protected readonly SteamService SteamService;
    protected ReposModsData _reposModsDataCache;


    public RepoService(IConfiguration configuration, SteamService steamService)
    {
        RepoUrl = SteamService.GetGame() == GameType.ZH ? configuration["Repos:ZH"] : configuration["Repos:Gen"];
        SteamService = steamService;
        SteamService.GetGeneralsInstallDir();
    }

    public ReposModsData GetRepoData()
    {
        if (_reposModsDataCache == null)
        {
            SteamService.CreateModsFolder();
            _reposModsDataCache = (new Deserializer()).Deserialize<ReposModsData>(Extensions.DownloadYaml(RepoUrl));
            _reposModsDataCache.modDatas = _reposModsDataCache.modDatas.OrderBy(x => x.ModName).ToList();
        }
        return _reposModsDataCache;
    }


    
    
}