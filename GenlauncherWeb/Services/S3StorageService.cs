using Minio;
using Minio.ApiEndpoints;
using Minio.DataModel.Args;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GenLauncherWeb.Middleware;
using GenLauncherWeb.Models;
using Microsoft.Extensions.Configuration;

namespace GenLauncherWeb.Services;

public class S3StorageService
{
    // Community-shared read-only fallback keys (inherited from the original
    // GenLauncher), used when a mod's repo data carries no credentials of its own.
    private readonly string _defaultPublicKey;
    private readonly string _defaultSecretKey;
    private readonly IHttpClientFactory _httpClientFactory;

    private const string DownloadClientName = "download";

    public S3StorageService(IConfiguration configuration, IHttpClientFactory httpClientFactory = null)
    {
        _defaultPublicKey = configuration["Extra:DefaultS3PublicKey"];
        _defaultSecretKey = configuration["Extra:DefaultS3SecretKey"];
        _httpClientFactory = httpClientFactory;
    }

    private HttpClient CreateDownloadClient()
        => _httpClientFactory?.CreateClient(DownloadClientName)
           ?? new HttpClient { Timeout = TimeSpan.FromHours(2) };

    public List<ModificationFileInfo> GetFilesForModData(ModData modData)
    {
        var current = new CultureInfo("en-US");
        current.DateTimeFormat = new DateTimeFormatInfo();
        current.DateTimeFormat.Calendar = new GregorianCalendar();
        Thread.CurrentThread.CurrentCulture = current;

        var pubKey = string.IsNullOrEmpty(modData.S3HostPublicKey) ? _defaultPublicKey : modData.S3HostPublicKey;
        var secKey = string.IsNullOrEmpty(modData.S3HostSecretKey) ? _defaultSecretKey : modData.S3HostSecretKey;
        var minioClient = new MinioClient()
            .WithEndpoint(modData.S3HostLink)
            .WithCredentials(pubKey, secKey)
            .Build();

        return GetFilesFromBucket(modData, minioClient);
    }

    public async Task DownloadS3FileToPathForModData(ModData modData, string filename, string destinationPath, Action<int> onChunk = null)
    {
        var downloadUrl = string.Format("https://{0}/{1}/{2}/{3}", modData.S3HostLink.Split(':')[0],
            modData.S3BucketName, modData.S3FolderName, filename);

        var client = CreateDownloadClient();
        using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
            throw new ApiException(502, "Failed to download file: " + filename);

        await using var contentStream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = File.Create(destinationPath);
        var buffer = new byte[81920];
        int read;
        while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, read);
            onChunk?.Invoke(read);
        }
    }

    public List<ModificationFileInfo> GetModFiles(Mod mod)
    {
        var current = new CultureInfo("en-US");
        current.DateTimeFormat = new DateTimeFormatInfo();
        current.DateTimeFormat.Calendar = new GregorianCalendar();
        Thread.CurrentThread.CurrentCulture = current;

        var pubKey = string.IsNullOrEmpty(mod.ModData.S3HostPublicKey) ? _defaultPublicKey : mod.ModData.S3HostPublicKey;
        var secKey = string.IsNullOrEmpty(mod.ModData.S3HostSecretKey) ? _defaultSecretKey : mod.ModData.S3HostSecretKey;
        var minioClient = new MinioClient()
            .WithEndpoint(mod.ModData.S3HostLink)
            .WithCredentials(pubKey, secKey)
            .Build();

        return GetFilesFromBucket(mod.ModData, minioClient);
    }

    private List<ModificationFileInfo> GetFilesFromBucket(ModData modData, IMinioClient minioClient)
    {
        var fileList = new List<ModificationFileInfo>();
        var args = new ListObjectsArgs()
            .WithBucket(modData.S3BucketName)
            .WithPrefix(modData.S3FolderName)
            .WithRecursive(true);

        try
        {
            Task.Run(async () =>
            {
                await foreach (var item in minioClient.ListObjectsEnumAsync(args))
                    fileList.Add(new ModificationFileInfo(
                        item.Key.Replace(modData.S3FolderName + '/', ""), item.ETag, item.Size));
            }).GetAwaiter().GetResult();
        }
        catch (Exception ex) when (ex is not ApiException)
        {
            throw new ApiException(502, "Cannot enumerate objects in S3 storage");
        }

        return fileList;
    }

    /// <summary>
    /// Streams an S3-hosted mod file straight to disk, reporting each chunk through
    /// the callback so download progress (and speed) can be tracked live.
    /// </summary>
    public async Task DownloadS3FileToPath(Mod mod, string filename, string destinationPath, Action<int> onChunk = null)
    {
        var downloadUrl = string.Format("https://{0}/{1}/{2}/{3}", mod.ModData.S3HostLink.Split(':')[0],
            mod.ModData.S3BucketName, mod.ModData.S3FolderName, filename);

        var client = CreateDownloadClient();
        using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(502, "Failed to download file: " + filename);
        }

        await using var contentStream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = File.Create(destinationPath);
        var buffer = new byte[81920];
        int read;
        while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, read);
            onChunk?.Invoke(read);
        }
    }
}

public class ModificationFileInfo
{
    public string FileName;
    public string Hash;
    public ulong Size;

    public ModificationFileInfo(string name, string hash)
    {
        FileName = name;
        Hash = hash;
    }

    public ModificationFileInfo(string name, string hash, ulong size)
    {
        FileName = name;
        Hash = hash;
        Size = size;
    }

    public override bool Equals(object obj)
    {
        if (obj is ModificationFileInfo modInfo && string.Equals(Hash, modInfo.Hash, StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(FileName, modInfo.FileName, StringComparison.OrdinalIgnoreCase))
                return true;

            var filename1 = Path.ChangeExtension(modInfo.FileName, "");
            var filename2 = Path.ChangeExtension(FileName, "");

            if (string.Equals(filename1, filename2, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return (FileName.ToUpper() + Hash.ToUpper()).GetHashCode();
    }
}
