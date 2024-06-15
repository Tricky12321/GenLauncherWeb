using System;
using System.Linq;
using System.Net.Http;
using GenLauncherWeb.Enums;
using GenLauncherWeb.Models;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.Configuration;
using YamlDotNet.Serialization;

namespace GenLauncherWeb.Services;

public class RepoService
{
    protected readonly string RepoUrl;
    protected readonly GameType GameType = GetGameType();
    protected readonly SteamService SteamService;
    protected ReposModsData _reposModsDataCache;


    public RepoService(IConfiguration configuration, SteamService steamService)
    {
        RepoUrl = GameType == GameType.ZH ? configuration["Repos:ZH"] : configuration["Repos:Gen"];
        SteamService = steamService;
        SteamService.GetGeneralInstallDir();
    }

    private static GameType GetGameType()
    {
        // TODO: Implement way to detect game, maybe some way to select which game to mod for
        return GameType.ZH;
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
            var deSerializer = new Deserializer();

            var idCounter = 1;
            _reposModsDataCache = deSerializer.Deserialize<ReposModsData>(DownloadRepoYaml());
            _reposModsDataCache.modDatas = _reposModsDataCache.modDatas.OrderBy(x => x.ModName).Select(x =>
            {
                x.ModId = idCounter++;
                return x;
            }).ToList();
        }
        return _reposModsDataCache;
    }

    public void DownloadAndInstallModByName(string name)
    {
        var repoData = GetRepoData();
        var mod = repoData.modDatas.FirstOrDefault(x => String.Equals(x.ModName.Trim(), name.Trim(), StringComparison.CurrentCultureIgnoreCase));
        
    }
    
    
}