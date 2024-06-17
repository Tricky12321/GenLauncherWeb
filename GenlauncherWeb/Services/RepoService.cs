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



    private string DownloadRepoYaml()
    {
        SteamService.CreateModsFolder();
        var rawRepoYaml = "";
        using (var client = new HttpClient())
        {
            client.GetAsync(RepoUrl).GetAwaiter().GetResult();
            rawRepoYaml = client.GetStringAsync(RepoUrl).GetAwaiter().GetResult();
        }

        return rawRepoYaml;
    }

    public ReposModsData GetRepoData()
    {
        if (_reposModsDataCache == null)
        {
            _reposModsDataCache = (new Deserializer()).Deserialize<ReposModsData>(DownloadRepoYaml());
            _reposModsDataCache.modDatas = _reposModsDataCache.modDatas.OrderBy(x => x.ModName).ToList();
        }
        return _reposModsDataCache;
    }


    
    
}