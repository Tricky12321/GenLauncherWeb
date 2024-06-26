using Minio;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using GenLauncherWeb.Models;

namespace GenLauncherWeb.Services;

public class S3StorageService
{
    public const string GenInsavePKey = "S58TYR9ISEZV8PBP8QG1";
    public const string GenInsaveSKey = "b2RU1oqVU5toJRnb4gODrXX8sBSgoLcHRX6qPWxj";

    public List<ModificationFileInfo> GetModFiles(Mod mod)
    {
        var current = new CultureInfo("en-US");
        current.DateTimeFormat = new DateTimeFormatInfo();
        current.DateTimeFormat.Calendar = new GregorianCalendar();

        Thread.CurrentThread.CurrentCulture = current;
        MinioClient minioClient;
        if (string.IsNullOrEmpty(mod.ModData.S3HostPublicKey) || String.IsNullOrEmpty(mod.ModData.S3HostSecretKey))
            minioClient = new MinioClient(mod.ModData.S3HostLink, GenInsavePKey, GenInsaveSKey);
        else
            minioClient = new MinioClient(mod.ModData.S3HostLink, mod.ModData.S3HostPublicKey, mod.ModData.S3HostSecretKey);

        var files = GetFilesFromBucket(mod.ModData, minioClient);
        return files;
    }

    private List<ModificationFileInfo> GetFilesFromBucket(ModData modData, MinioClient minioClient)
    {
        var getListBucketsTask = minioClient.ListBucketsAsync().GetAwaiter().GetResult();

        var filestList = new List<ModificationFileInfo>();

        bool finished = false;

        var result = minioClient.ListObjectsAsync(modData.S3BucketName, modData.S3FolderName, true);

        var subscription = result.Subscribe(
            item =>
            {
                filestList.Add(new ModificationFileInfo(item.Key.Replace(modData.S3FolderName + '/', ""), item.ETag, item.Size));
            },
            ex => throw new Exception("Cannot enumerate objects in S3 storage"),
            () => finished = true);


        while (!finished)
        {
            Thread.Sleep(100);
        }

        return filestList;
    }

    public async Task<byte[]> DownloadS3File(string filename, Mod mod)
    {
        var downloadUrl = string.Format("https://{0}/{1}/{2}/{3}", mod.ModData.S3HostLink.Split(':')[0],
            mod.ModData.S3BucketName, mod.ModData.S3FolderName, filename);
        
        var mes = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
        byte[] bytes;

        using (var client = new HttpClient())
        {
            var result = await client.SendAsync(mes);
            if (result.IsSuccessStatusCode)
            {
                bytes = await result.Content.ReadAsByteArrayAsync();
            }
            else
            {
                throw new Exception("Failed to download file: " + filename);
            }
        }

        return bytes;
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
        if (obj is ModificationFileInfo modInfo && String.Equals(Hash, modInfo.Hash, StringComparison.OrdinalIgnoreCase))
        {
            if (String.Equals(FileName, modInfo.FileName, StringComparison.OrdinalIgnoreCase))
                return true;

            var filename1 = Path.ChangeExtension(modInfo.FileName, "");
            var filename2 = Path.ChangeExtension(FileName, "");

            if (String.Equals(filename1, filename2, StringComparison.OrdinalIgnoreCase))
                return true;
        }
                
        return false;
    }

    public override int GetHashCode()
    {
        return (FileName.ToUpper() + Hash.ToUpper()).GetHashCode();
    }
}