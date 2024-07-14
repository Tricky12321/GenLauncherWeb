using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using GenLauncherWeb.Models;
using HtmlAgilityPack;
using SevenZipExtractor;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
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

    public static string ParseDownloadLink(this string link)
    {
        //replaced dl=0 to dl=1 to get download link
        //MyListBoxData.Add(new Modification("Rise of The Reds", "1.87 PB 2.0", "1.87 PB 2.0", "https://www.dropbox.com/s/nh8n8axi95gge41/ROTR.7z?dl=1", new BitmapImage(new Uri("Images/1.png", UriKind.Relative))));
        //generate from https://onedrive.live.com/?authkey=%21AIWtLuu54V5qKQ4&cid=896C9369E9176506&id=896C9369E9176506%21464&parId=896C9369E9176506%21463&o=OneUp
        //MyListBoxData.Add(new Modification("TEOD", "0.97.5", "0.97.5", "https://onedrive.live.com/download?cid=896C9369E9176506&resid=896C9369E9176506%21464&authkey=%21AIWtLuu54V5qKQ4"));
        //https://www.dropbox.com/s/ec9fjg909fkrvtt/TEOD.7z?dl=0            

        if (link.Contains("dropbox.com"))
        {
            link = link.Replace("?dl=0", "?dl=1");
            return link;
        }

        if (link.Contains("onedrive.live.com"))
        {
            if (link.Contains("embed"))
            {
                return link.Replace("embed", "download");
            }

            var linkParts = link.Replace("https://onedrive.live.com/?", string.Empty).Split('&').ToList();

            var cid = linkParts.Where(t => t.Contains("cid=")).Select(t => t.Replace("cid=", string.Empty)).FirstOrDefault();
            var authKey = linkParts.Where(t => t.Contains("authkey=")).Select(t => t.Replace("authkey=", string.Empty)).FirstOrDefault();
            var resid = linkParts.Where(t => t.Contains("id=") && !t.Contains("cid=")).Select(t => t.Replace("id=", string.Empty)).FirstOrDefault();

            return String.Format("https://onedrive.live.com/download?cid={0}&resid={1}&authkey={2}", cid, resid, authKey);
        }

        return link;
    }

    public static bool HasS3Storage(this Mod mod)
    {
        return !string.IsNullOrEmpty(mod.ModData.S3HostLink) && !string.IsNullOrEmpty(mod.ModData.S3FolderName) && !string.IsNullOrEmpty(mod.ModData.S3BucketName);
    }

    public static string CleanString(this string input)
    {
        // Remove non-ASCII characters
        string asciiOnly = Regex.Replace(input, @"[^\x00-\x7F]", "");

        // Remove special characters except a-z and A-Z, replace spaces with underscores
        string cleaned = Regex.Replace(asciiOnly, @"[^a-zA-Z\s]", "");
        cleaned = Regex.Replace(cleaned, @"\s+", "_");

        return cleaned;
    }

    public static bool CreateFolderIfItDoesNotExist(this string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            return true;
        }

        return false;
    }


    public static string GetMd5HashOfFile(this string path)
    {
        using (var md5 = MD5.Create())
        {
            using (var stream = File.OpenRead(path))
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }

    public static bool UninstallMod(this Mod mod)
    {
        Directory.Delete(mod.ModDir, true);
        mod.ModDir = "";
        mod.TotalSize = 0;
        mod.DownloadedFiles = new List<string>();
        mod.DownloadedVersion = "";
        mod.Installed = false;
        return true;
    }

    public static string EscapeLinuxPath(this string path)
    {
        var escapedPath = new StringBuilder();
        escapedPath.Append('\'');

        foreach (char c in path)
        {
            if (c == '\'')
            {
                escapedPath.Append("'\\''");
            }
            else
            {
                escapedPath.Append(c);
            }
        }

        escapedPath.Append('\'');
        return escapedPath.ToString();
    }

    public static string FindFileRecursively(string folderPath, string fileName)
    {
        try
        {
            foreach (string file in Directory.GetFiles(folderPath))
            {
                if (Path.GetFileName(file).Equals(fileName, StringComparison.OrdinalIgnoreCase))
                {
                    return file;
                }
            }

            foreach (string directory in Directory.GetDirectories(folderPath))
            {
                string foundFile = FindFileRecursively(directory, fileName);
                if (foundFile != null)
                {
                    return foundFile;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }

        return null;
    }

    public static bool IsSymbolicLink(string path)
    {
        FileInfo fileInfo = new FileInfo(path);
        return fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
    }

    private static readonly Dictionary<string, string> ArchiveMimeTypeMappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
    {
        { "application/zip", ".zip" },
        { "application/x-7z-compressed", ".7z" },
        { "application/x-rar-compressed", ".rar" },
        { "application/gzip", ".gz" },
        { "application/x-tar", ".tar" },
        { "application/x-bzip2", ".bz2" },
        { "application/x-lzip", ".lz" },
        { "application/x-xz", ".xz" },
        { "application/x-shar", ".shar" },
        { "application/x-szip", ".sz" },
    };

    public static string GetFileExtensionFromMimeType(this string mimeType)
    {
        if (ArchiveMimeTypeMappings.TryGetValue(mimeType, out string extension))
        {
            return extension;
        }

        return null; // Or some default extension like ".bin"
    }

    public static bool ExtractFile(this string fileName)
    {
        var fileFolder = Path.GetDirectoryName(fileName);
        var fileExtension = Path.GetExtension(fileName);
        switch (fileExtension)
        {
            case ".zip":
                return ExtractZipFile(fileName, fileFolder, true);
            case ".7z":
                return Extract7zFile(fileName, fileFolder, true);
            case ".rar":
                return ExtractRarFile(fileName, fileFolder, true);
            default:
                return false;
        }
    }

    private static bool ExtractRarFile(string fileName, string destination, bool deleteFile)
    {
        
        using (RarArchive archive = RarArchive.Open(fileName))
        {
            foreach (RarArchiveEntry entry in archive.Entries)
            {
                if (!entry.IsDirectory)
                {
                    string filePath = Path.Combine(destination, entry.Key);
                    Console.WriteLine($"Extracting: {entry.Key}");

                    Path.GetDirectoryName(filePath).CreateFolderIfItDoesNotExist();
                    // Extract the file
                    entry.WriteToFile(filePath);
                    if (!File.Exists(filePath))
                    {
                        return false;
                    }
                }
            }
        }
        
        if (deleteFile)
        {
            File.Delete(fileName);
        }

        return true;
    }

    private static bool Extract7zFile(string fileName, string destination, bool deleteFile)
    {
        using (ArchiveFile archiveFile = new ArchiveFile(fileName))
        {
            foreach (Entry entry in archiveFile.Entries)
            {
                // Extract each entry to the specified directory
                entry.Extract(destination);
                Console.WriteLine($"7z: Extracted: {entry.FileName}");
                var extractedFilePath = Path.Combine(destination, entry.FileName);
                if (!File.Exists(extractedFilePath))
                {
                    return false;
                }
            }
        }

        if (deleteFile)
        {
            File.Delete(fileName);
        }

        return true;
    }

    private static bool ExtractZipFile(string fileName, string destination, bool deleteFile)
    {
        ZipFile.ExtractToDirectory(fileName, destination);
        if (deleteFile)
        {
            File.Delete(fileName);
        }

        return true;
    }
    
    public static List<string> GetAllFilesRecursively(string path)
    {
        List<string> files = new List<string>();
        
        try
        {
            // Get all files in the directory and subdirectories
            files.AddRange(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        catch (DirectoryNotFoundException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        return files;
    }
    
    public static long GetTotalSizeInBytes(List<string> filePaths)
    {
        long totalSize = 0;

        foreach (string filePath in filePaths)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    FileInfo fileInfo = new FileInfo(filePath);
                    totalSize += fileInfo.Length;
                }
                else
                {
                    Console.WriteLine($"File not found: {filePath}");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Error accessing file {filePath}: {ex.Message}");
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"File not found: {filePath} - {ex.Message}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error reading file {filePath}: {ex.Message}");
            }
        }

        return totalSize;
    }
}