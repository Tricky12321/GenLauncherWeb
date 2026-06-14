using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GenLauncherWeb.Enums;
using GenLauncherWeb.Middleware;
using GenLauncherWeb.Models;
using Newtonsoft.Json;

namespace GenLauncherWeb.Services;

public class PatchService
{
    private readonly RepoService _repoService;
    private readonly S3StorageService _s3StorageService;
    private readonly ModService _modService;
    private readonly OptionsService _optionsService;

    private readonly object _lock = new();
    private readonly Dictionary<GameType, List<GamePatch>> _patchLists = new();
    private readonly ConcurrentDictionary<string, ModDownloadProgress> _downloadProgress = new();

    private const string BackupDir = "Patches";

    public PatchService(RepoService repoService, S3StorageService s3StorageService, ModService modService, OptionsService optionsService)
    {
        _repoService = repoService;
        _s3StorageService = s3StorageService;
        _modService = modService;
        _optionsService = optionsService;
    }

    // ---------------------------------------------------------------- paths

    private GameType SelectedGame => _optionsService.GetOptions().SelectedGame;

    private string GetPatchListFilePath(GameType game)
    {
        var dir = _modService.GetModStorageDir();
        var fileName = game == GameType.Gen ? "genlauncher_patches_gen.json" : "genlauncher_patches_zh.json";
        return Path.Combine(dir, fileName);
    }

    private string GetPatchStorageDir(string cleanedPatchName)
        => Path.Combine(_modService.GetModStorageDir(), "Patches", cleanedPatchName);

    private string GetPatchBackupDir(GameType game, string cleanedPatchName)
        => Path.Combine(_modService.GetModStorageDir(), ModService.BackupOriginalGameFilesDir, game.ToString(), BackupDir, cleanedPatchName);

    private string GetGameDir(GameType game)
        => SteamService.GetGameInstallDir(game, _optionsService.GetOptions().SteamPath);

    // ---------------------------------------------------------------- list

    private List<GamePatch> GetPatchList(GameType game)
    {
        if (_patchLists.TryGetValue(game, out var list))
            return list;

        var filePath = GetPatchListFilePath(game);
        list = File.Exists(filePath)
            ? JsonConvert.DeserializeObject<List<GamePatch>>(File.ReadAllText(filePath)) ?? new List<GamePatch>()
            : new List<GamePatch>();

        _patchLists[game] = list;
        return list;
    }

    private void SavePatchList(GameType game)
    {
        var filePath = GetPatchListFilePath(game);
        File.WriteAllText(filePath, JsonConvert.SerializeObject(_patchLists[game], Formatting.Indented));
    }

    // ---------------------------------------------------------------- public API

    public List<GamePatchDto> GetPatches()
    {
        var game = SelectedGame;
        var repoUrls = _repoService.GetRepoData(game)?.originalGamePatches ?? new List<string>();

        lock (_lock)
        {
            var list = GetPatchList(game);

            // Add any new URLs from the repo that we haven't seen before
            bool changed = false;
            foreach (var url in repoUrls)
            {
                if (list.Any(p => p.PatchUrl == url))
                    continue;

                GamePatch patch;
                try
                {
                    var data = Extensions.DownloadModDataFromUrl(url);
                    patch = new GamePatch
                    {
                        PatchUrl = url,
                        Name = data.Name,
                        Version = data.Version,
                        UIImageSourceLink = data.UIImageSourceLink,
                        PatchData = data
                    };
                }
                catch
                {
                    patch = new GamePatch { PatchUrl = url, Name = url };
                }

                list.Add(patch);
                changed = true;
            }

            if (changed)
                SavePatchList(game);

            return list
                .Where(p => repoUrls.Contains(p.PatchUrl))
                .Select(p => ToDto(p))
                .ToList();
        }
    }

    public async Task DownloadPatch(string patchUrl)
    {
        var game = SelectedGame;
        GamePatch patch;
        lock (_lock)
        {
            patch = FindPatch(game, patchUrl);
            if (patch.Downloading)
                throw new ApiException(409, "This patch is already being downloaded.");
            patch.Downloading = true;
        }

        var cleanedName = (patch.Name ?? "patch").CleanString();
        var progressKey = ProgressKey(patchUrl);

        try
        {
            var patchDir = GetPatchStorageDir(cleanedName);
            patchDir.CreateFolderIfItDoesNotExist();

            // Ensure we have the YAML data (may have been loaded without it on a previous run)
            if (patch.PatchData == null)
            {
                patch.PatchData = Extensions.DownloadModDataFromUrl(patchUrl);
                patch.Name = patch.PatchData.Name ?? patch.Name;
                patch.Version = patch.PatchData.Version;
                patch.UIImageSourceLink = patch.PatchData.UIImageSourceLink;
            }

            List<string> files;
            ulong size;
            if (HasS3(patch))
                (files, size) = await DownloadFromS3(patch, patchDir, progressKey);
            else
                (files, size) = await DownloadFromSimpleLink(patch, patchDir, progressKey);

            lock (_lock)
            {
                patch.Downloaded = true;
                patch.PatchDir = patchDir;
                patch.DownloadedFiles = files;
                patch.TotalSize = size;
                SavePatchList(game);
            }

            if (_downloadProgress.TryGetValue(progressKey, out var prog))
                prog.Downloaded = true;
        }
        catch
        {
            _downloadProgress.TryRemove(progressKey, out _);
            throw;
        }
        finally
        {
            lock (_lock)
            {
                patch.Downloading = false;
            }
        }
    }

    public ModDownloadProgress GetDownloadProgress(string patchUrl)
    {
        var key = ProgressKey(patchUrl);
        if (_downloadProgress.TryGetValue(key, out var progress))
        {
            if (progress.Downloaded)
                _downloadProgress.TryRemove(key, out _);
            return progress;
        }
        return new ModDownloadProgress { DownloadedSize = 0, TotalDownloadSize = 0, Downloaded = false };
    }

    public void InstallPatch(string patchUrl)
    {
        var game = SelectedGame;
        lock (_lock)
        {
            var patch = FindPatch(game, patchUrl);

            if (!patch.Downloaded || patch.DownloadedFiles == null)
                throw new ApiException(409, "The patch must be downloaded before it can be installed.");

            if (patch.Installed) return;

            var options = _optionsService.GetOptions();
            InstallCore(game, patch, options);
            patch.Installed = true;
            SavePatchList(game);
        }
    }

    public void UninstallPatch(string patchUrl)
    {
        var game = SelectedGame;
        lock (_lock)
        {
            var patch = FindPatch(game, patchUrl);
            if (!patch.Installed) return;

            UninstallCore(game, patch);
            patch.Installed = false;
            SavePatchList(game);
        }
    }

    public void DeletePatch(string patchUrl)
    {
        var game = SelectedGame;
        lock (_lock)
        {
            var patch = FindPatch(game, patchUrl);
            if (patch.Installed)
                throw new ApiException(409, "Uninstall the patch before deleting its files.");

            if (!string.IsNullOrEmpty(patch.PatchDir))
            {
                try { Directory.Delete(patch.PatchDir, true); } catch (DirectoryNotFoundException) { }
            }

            patch.Downloaded = false;
            patch.PatchDir = null;
            patch.DownloadedFiles = null;
            patch.TotalSize = 0;
            SavePatchList(game);
        }
    }

    // ---------------------------------------------------------------- install/uninstall core

    private void InstallCore(GameType game, GamePatch patch, LauncherOptions options)
    {
        var gameFolder = GetGameDir(game);
        var cleanedName = (patch.Name ?? "patch").CleanString();

        foreach (var file in patch.DownloadedFiles)
        {
            var fixedFile = file.FixModFileName();
            var gameFilePath = Path.Combine(gameFolder, fixedFile);
            if (File.Exists(gameFilePath))
            {
                var backupDest = Path.Combine(GetPatchBackupDir(game, cleanedName), fixedFile);
                Path.GetDirectoryName(backupDest).CreateFolderIfItDoesNotExist();
                File.Move(gameFilePath, backupDest, true);
            }

            var sourceFile = Path.Combine(patch.PatchDir, file);
            if (!File.Exists(sourceFile)) continue;
            Path.GetDirectoryName(gameFilePath).CreateFolderIfItDoesNotExist();
            switch (options.InstallMethod)
            {
                case InstallMethod.CopyFiles:
                    File.Copy(sourceFile, gameFilePath, true);
                    break;
                case InstallMethod.SymLink:
                    SymLinkService.CreateSymbolicLink(gameFilePath, sourceFile);
                    break;
            }
        }
    }

    private void UninstallCore(GameType game, GamePatch patch)
    {
        var gameFolder = GetGameDir(game);
        var cleanedName = (patch.Name ?? "patch").CleanString();
        var backupFolder = GetPatchBackupDir(game, cleanedName);

        foreach (var file in patch.DownloadedFiles ?? new List<string>())
        {
            var fixedFile = file.FixModFileName();
            var gameFilePath = Path.Combine(gameFolder, fixedFile);
            var backupFile = Path.Combine(backupFolder, fixedFile);
            if (File.Exists(backupFile))
            {
                Path.GetDirectoryName(gameFilePath).CreateFolderIfItDoesNotExist();
                File.Move(backupFile, gameFilePath, true);
            }
            else if (File.Exists(gameFilePath))
            {
                File.Delete(gameFilePath);
            }
        }
    }

    // ---------------------------------------------------------------- download helpers

    private static bool HasS3(GamePatch patch)
        => patch.PatchData != null
           && !string.IsNullOrEmpty(patch.PatchData.S3HostLink)
           && !string.IsNullOrEmpty(patch.PatchData.S3FolderName)
           && !string.IsNullOrEmpty(patch.PatchData.S3BucketName);

    private async Task<(List<string> files, ulong size)> DownloadFromS3(GamePatch patch, string patchDir, string progressKey)
    {
        var fileList = _s3StorageService.GetFilesForModData(patch.PatchData);
        ulong total = 0;
        fileList.ForEach(x => total += x.Size);

        var progress = new ModDownloadProgress
        {
            TotalDownloadSize = total,
            FileList = fileList.Select(x => x.FileName).ToList(),
            DownloadedFiles = new List<string>(),
            DownloadedSize = 0,
            Downloaded = false
        };
        _downloadProgress[progressKey] = progress;

        var installedFiles = new List<string>();
        ulong installedSize = 0;
        foreach (var file in fileList)
        {
            var filePath = Path.Combine(patchDir, file.FileName);
            if (File.Exists(filePath) && filePath.GetMd5HashOfFile() == file.Hash)
            {
                progress.DownloadedFiles.Add(file.FileName);
                progress.DownloadedSize += file.Size;
                installedFiles.Add(file.FileName);
                installedSize += file.Size;
                continue;
            }

            if (File.Exists(filePath)) File.Delete(filePath);
            Path.GetDirectoryName(filePath).CreateFolderIfItDoesNotExist();
            await _s3StorageService.DownloadS3FileToPathForModData(patch.PatchData, file.FileName, filePath,
                read => progress.DownloadedSize += (ulong)read);

            if (filePath.GetMd5HashOfFile() == file.Hash)
            {
                progress.DownloadedFiles.Add(file.FileName);
                installedFiles.Add(file.FileName);
                installedSize += file.Size;
            }
            else if (!filePath.ToLower().Contains("changelog") && Path.GetExtension(filePath).ToLower() != ".txt")
            {
                throw new ApiException(502, "Hash mismatch for patch file: " + file.FileName);
            }
        }

        return (installedFiles, installedSize);
    }

    private async Task<(List<string> files, ulong size)> DownloadFromSimpleLink(GamePatch patch, string patchDir, string progressKey)
    {
        var progress = new ModDownloadProgress
        {
            TotalDownloadSize = 0,
            FileList = new List<string>(),
            DownloadedFiles = new List<string>(),
            DownloadedSize = 0,
            Downloaded = false
        };
        _downloadProgress[progressKey] = progress;

        var link = patch.PatchData.SimpleDownloadLink.ParseDownloadLink();
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromHours(2);
        using var response = await client.GetAsync(link, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        progress.TotalDownloadSize = (ulong)(response.Content.Headers.ContentLength ?? 0);
        var extension = response.Content.Headers.ContentType?.MediaType?.GetFileExtensionFromMimeType()
                        ?? Path.GetExtension(new Uri(link).AbsolutePath);
        var archivePath = Path.Combine(patchDir, Path.GetFileName(patchDir) + extension);

        await using (var contentStream = await response.Content.ReadAsStreamAsync())
        await using (var fileStream = File.Create(archivePath))
        {
            var buffer = new byte[81920];
            int read;
            while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, read);
                progress.DownloadedSize += (ulong)read;
            }
        }

        if (!archivePath.ExtractFile())
            throw new ApiException(502, "Patch archive could not be extracted: " + Path.GetFileName(archivePath));

        var allFiles = Extensions.GetAllFilesRecursively(patchDir);
        var installedFiles = allFiles.Select(f => Path.GetRelativePath(patchDir, f)).ToList();
        var totalSize = (ulong)Extensions.GetTotalSizeInBytes(allFiles);

        progress.DownloadedFiles = installedFiles;
        progress.DownloadedSize = totalSize;
        if (progress.TotalDownloadSize == 0) progress.TotalDownloadSize = totalSize;

        return (installedFiles, totalSize);
    }

    // ---------------------------------------------------------------- helpers

    private GamePatch FindPatch(GameType game, string patchUrl)
    {
        var patch = GetPatchList(game).FirstOrDefault(p => p.PatchUrl == patchUrl);
        if (patch == null)
            throw new ApiException(404, "Patch not found: " + patchUrl);
        return patch;
    }

    private static string ProgressKey(string patchUrl)
        => "patch_" + patchUrl.StandardModName();

    private static GamePatchDto ToDto(GamePatch p) => new()
    {
        PatchUrl = p.PatchUrl,
        Name = p.Name,
        Version = p.Version,
        UIImageSourceLink = p.UIImageSourceLink,
        Downloaded = p.Downloaded,
        Installed = p.Installed,
        Downloading = p.Downloading,
        TotalSize = p.TotalSize
    };
}
