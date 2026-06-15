using System.IO;
using System.Net.Http;
using GenLauncherWeb.Models;
using YamlDotNet.Serialization;

namespace GenLauncherWeb;

/// <summary>
/// Fetches and deserializes the small YAML metadata documents (repo lists, per-mod
/// data). These are needed from plain POCOs (e.g. <see cref="Mod"/>) that cannot take a
/// DI dependency, so a single shared <see cref="HttpClient"/> is used instead of one per
/// call — avoiding the socket exhaustion that <c>new HttpClient()</c>-per-call causes.
///
/// The synchronous <see cref="HttpClient.Send(HttpRequestMessage)"/> API is used (rather
/// than blocking on an async call with <c>.GetAwaiter().GetResult()</c>) so no thread is
/// parked waiting on its own async continuation.
/// </summary>
public static class MetadataDownloader
{
    private static readonly HttpClient Client = new();

    public static string DownloadYaml(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = Client.Send(request);
        response.EnsureSuccessStatusCode();
        using var reader = new StreamReader(response.Content.ReadAsStream());
        return reader.ReadToEnd();
    }

    public static ModData DownloadModData(this ModAddonsAndPatches modInfo)
    {
        return DownloadModDataFromUrl(modInfo.ModLink);
    }

    public static ModData DownloadModDataFromUrl(string url)
    {
        var yaml = DownloadYaml(url);
        var deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build();
        return deserializer.Deserialize<ModData>(yaml);
    }
}
