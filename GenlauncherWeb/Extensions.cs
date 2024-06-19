using System.Net.Http;
using GenLauncherWeb.Models;
using YamlDotNet.Serialization;

namespace GenLauncherWeb;

public static class Extensions
{
    public static ModData DownloadModData(this ModAddonsAndPatches modInfo)
    {
        var modYaml = DownloadYaml(modInfo.ModLink);
        var deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build();
        var decodedYaml = deserializer.Deserialize<ModData>(modYaml);
        return decodedYaml;
    }
    
    public static string DownloadYaml(string url)
    {
        string data;
        using (var client = new HttpClient())
        {
            client.GetAsync(url).GetAwaiter().GetResult();
            data = client.GetStringAsync(url).GetAwaiter().GetResult();
        }
        return data;
    }
}