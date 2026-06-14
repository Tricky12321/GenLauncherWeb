using System.Collections.Generic;
using System.Linq;
using GenLauncherWeb.Enums;
using GenLauncherWeb.Models;
using Microsoft.Extensions.Configuration;
using YamlDotNet.Serialization;

namespace GenLauncherWeb.Services;

public class RepoService
{
    private readonly string _zhRepoUrl;
    private readonly string _genRepoUrl;
    private readonly Dictionary<GameType, ReposModsData> _cache = new();
    private readonly object _lock = new();

    public RepoService(IConfiguration configuration)
    {
        _zhRepoUrl = configuration["Repos:ZH"];
        _genRepoUrl = configuration["Repos:Gen"];
    }

    public ReposModsData GetRepoData(GameType game)
    {
        lock (_lock)
        {
            if (!_cache.TryGetValue(game, out var data))
            {
                var url = game == GameType.Gen ? _genRepoUrl : _zhRepoUrl;
                data = new Deserializer().Deserialize<ReposModsData>(Extensions.DownloadYaml(url));
                data.modDatas = data.modDatas.OrderBy(x => x.ModName).ToList();
                _cache[game] = data;
            }

            return data;
        }
    }
}
