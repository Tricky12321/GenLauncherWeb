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


    public RepoService(IConfiguration configuration, SteamService steamService)
    {
        RepoUrl = GameType == GameType.ZH ? configuration["Repos:ZH"] : configuration["Repos:Gen"];
        SteamService = steamService;
        SteamService.GetGeneralInstallDir();
    }

    public static GameType GetGameType()
    {
        // TODO: Implement way to detect game, maybe some way to select which game to mod for
        return GameType.ZH;
    }

    public string DownloadRepoYaml()
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
        var deSerializer = new Deserializer();
        return deSerializer.Deserialize<ReposModsData>(DownloadRepoYaml());
    }
}