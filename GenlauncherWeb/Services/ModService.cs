using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using GenLauncherWeb.Enums;
using GenLauncherWeb.Middleware;
using GenLauncherWeb.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GenLauncherWeb.Services;

public class ModService
{
    private readonly RepoService _repoService;
    private readonly SteamService _steamService;
    private readonly S3StorageService _s3StorageService;
    private readonly OptionsService _optionsService;
    private readonly IGamePaths _gamePaths;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ModService> _logger;

    private readonly object _lock = new();
    private readonly Dictionary<GameType, List<Mod>> _modLists = new();
    private readonly ConcurrentDictionary<string, ModDownloadProgress> _modInstallInfo = new();

    private const string DownloadClientName = "download";

    public readonly string ModdedGameExeDownloadLink;
    public readonly string ModdedGameExeHash;
    public readonly string GenToolDllHash;
    public readonly string GentoolDownloadLink;

    public ModService(RepoService repoService, SteamService steamService, S3StorageService s3StorageService,
        OptionsService optionsService, IGamePaths gamePaths, IHttpClientFactory httpClientFactory,
        ILogger<ModService> logger, IConfiguration configuration)
    {
        _repoService = repoService;
        _steamService = steamService;
        _s3StorageService = s3StorageService;
        _optionsService = optionsService;
        _gamePaths = gamePaths;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        ModdedGameExeDownloadLink = configuration["Extra:ModdedExeDownloadLink"];
        ModdedGameExeHash = configuration["Extra:ModdedGameExeHash"];
        GenToolDllHash = configuration["Extra:GentoolDllHash"];
        GentoolDownloadLink = configuration["Extra:GentoolDownloadLink"];
    }

    // ---------------------------------------------------------------- paths

    private GameType SelectedGame => _optionsService.GetOptions().SelectedGame;

    public string GetModStorageDir() => _gamePaths.ModStorageDir;

    private string GetGameDir(GameType game) => _gamePaths.GameDir(game);

    private string GetModListFilePath(GameType game)
    {
        return Path.Combine(GetModStorageDir(), StorageNames.ModListFile(game));
    }

    private string GetBackupFolder(GameType game) => _gamePaths.BackupRoot(game);

    // ------------------------------------------------------- list management

    /// <summary>
    /// Loads (once) and returns the mod list of a game. Callers must hold the lock.
    /// </summary>
    private List<Mod> GetModList(GameType game)
    {
        if (_modLists.TryGetValue(game, out var list))
        {
            return list;
        }

        var filePath = GetModListFilePath(game);
        if (game == GameType.ZH && !File.Exists(filePath))
        {
            // Older versions stored the (ZH-only) mod list in a shared file
            var legacyFile = Path.Combine(GetModStorageDir(), StorageNames.LegacyModListFile);
            if (File.Exists(legacyFile))
            {
                File.Move(legacyFile, filePath);
            }
        }

        list = File.Exists(filePath)
            ? JsonConvert.DeserializeObject<List<Mod>>(File.ReadAllText(filePath)) ?? new List<Mod>()
            : new List<Mod>();

        // Transient flags must not survive a restart; backfill the game tag for
        // entries written before it existed
        foreach (var mod in list)
        {
            mod.Downloading = false;
            mod.Installing = false;
            if (mod.Game != GameType.Gen && mod.Game != GameType.ZH)
            {
                mod.Game = game;
            }
        }

        _modLists[game] = list;
        return list;
    }

    private void UpdateModListFile(GameType game)
    {
        var json = JsonConvert.SerializeObject(_modLists[game]);
        File.WriteAllText(GetModListFilePath(game), json);
    }

    private Mod FindMod(GameType game, string modName)
    {
        var mod = GetModList(game).FirstOrDefault(x => string.Equals(x.ModInfo.ModName.Trim(), modName.Trim(), StringComparison.CurrentCultureIgnoreCase));
        if (mod == null)
        {
            throw new ApiException(404, $"Mod '{modName}' is not in the mod list.");
        }

        return mod;
    }

    public List<Mod> GetAddedMods()
    {
        lock (_lock)
        {
            return GetModList(SelectedGame);
        }
    }

    /// <summary>
    /// All repository mods of the selected game, flagged with whether they are
    /// already in the user's list and whether they are installed.
    /// </summary>
    public ReposModsData GetBrowseMods()
    {
        var game = SelectedGame;
        var repoData = _repoService.GetRepoData(game);
        lock (_lock)
        {
            var addedMods = GetModList(game);
            // Return a copy: the cached repo data must not be mutated
            return new ReposModsData
            {
                Game = game,
                LauncherVersion = repoData.LauncherVersion,
                DownloadLink = repoData.DownloadLink,
                VulkanReposData = repoData.VulkanReposData,
                modDatas = repoData.modDatas.Select(x =>
                {
                    var addedMod = addedMods.FirstOrDefault(y => y.ModInfo.ModName == x.ModName);
                    return new ModAddonsAndPatches
                    {
                        ModId = x.ModId,
                        ModName = x.ModName,
                        ModLink = x.ModLink,
                        ModPatches = x.ModPatches,
                        ModAddons = x.ModAddons,
                        Added = addedMod != null,
                        Downloaded = addedMod?.Downloaded ?? false,
                        Installed = addedMod?.Installed ?? false
                    };
                }).ToList(),
                globalAddonsData = repoData.globalAddonsData,
                originalGameAddons = repoData.originalGameAddons,
                originalGamePatches = repoData.originalGamePatches,
                AdvData = repoData.AdvData
            };
        }
    }

    public void AddModToModList(string modName)
    {
        var game = SelectedGame;
        var repoData = _repoService.GetRepoData(game);
        lock (_lock)
        {
            var modList = GetModList(game);
            var mod = repoData.modDatas.FirstOrDefault(x => string.Equals(x.ModName.Trim(), modName.Trim(), StringComparison.CurrentCultureIgnoreCase));
            if (mod == null)
            {
                throw new ApiException(404, $"Mod '{modName}' was not found in the repository.");
            }

            if (modList.Any(x => x.ModInfo.ModName == mod.ModName))
            {
                return;
            }

            modList.Add(new Mod
            {
                Game = game,
                Installed = false,
                Downloaded = false,
                DownloadedVersion = "",
                ModInfo = mod
            });
            UpdateModListFile(game);
        }
    }

    public void RemoveModFromModList(string modName)
    {
        var game = SelectedGame;
        lock (_lock)
        {
            var modList = GetModList(game);
            var mod = FindMod(game, modName);
            if (mod.Installed)
            {
                throw new ApiException(409, "The mod is installed. Uninstall it before removing it from the list.");
            }

            // No orphaned files in mod storage
            DeleteModFilesCore(mod);
            modList.Remove(mod);
            UpdateModListFile(game);
        }
    }

    // ------------------------------------------------------------- download

    public async Task DownloadMod(string modName)
    {
        var game = SelectedGame;
        Mod mod;
        lock (_lock)
        {
            mod = FindMod(game, modName);
            if (mod.Downloading)
            {
                throw new ApiException(409, "This mod is already being downloaded.");
            }

            mod.Downloading = true;
            UpdateModListFile(game);
        }

        var progressKey = mod.ModInfo.ModName.StandardModName();
        try
        {
            var cleanedModName = mod.ModInfo.ModName.CleanString();
            var modDir = Path.Combine(GetModStorageDir(), cleanedModName);
            modDir.CreateFolderIfItDoesNotExist();

            List<string> installedFiles;
            ulong totalInstallSize;
            if (mod.HasS3Storage())
            {
                (installedFiles, totalInstallSize) = await DownloadModFromS3(mod, modDir, progressKey);
            }
            else
            {
                (installedFiles, totalInstallSize) = await DownloadModFromSimpleLink(mod, modDir, progressKey);
            }

            lock (_lock)
            {
                mod.TotalSize = totalInstallSize;
                mod.Downloaded = true;
                mod.DownloadedVersion = mod.ModData.Version;
                mod.DownloadedFiles = installedFiles;
                mod.ModDir = modDir;
                UpdateModListFile(game);
            }

            if (_modInstallInfo.TryGetValue(progressKey, out var progress))
            {
                progress.Downloaded = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download of mod {ModName} failed", modName);
            // Stop the poller from waiting on a download that died
            _modInstallInfo.TryRemove(progressKey, out _);
            throw;
        }
        finally
        {
            lock (_lock)
            {
                mod.Downloading = false;
                UpdateModListFile(game);
            }
        }
    }

    private async Task<(List<string> installedFiles, ulong totalSize)> DownloadModFromS3(Mod mod, string modDir, string progressKey)
    {
        var fileList = _s3StorageService.GetModFiles(mod);
        ulong totalSize = 0;
        fileList.ForEach(x => totalSize += x.Size);

        var progress = new ModDownloadProgress
        {
            TotalDownloadSize = totalSize,
            FileList = fileList.Select(x => x.FileName).ToList(),
            DownloadedFiles = new List<string>(),
            DownloadedSize = 0,
            Downloaded = false
        };
        _modInstallInfo[progressKey] = progress;

        var installedFiles = new List<string>();
        ulong totalInstallSize = 0;
        foreach (var file in fileList)
        {
            var filePath = Path.Combine(modDir, file.FileName);
            if (File.Exists(filePath))
            {
                if (filePath.GetMd5HashOfFile() == file.Hash)
                {
                    // Already downloaded earlier (resumed download)
                    progress.DownloadedFiles.Add(file.FileName);
                    progress.DownloadedSize += file.Size;
                    installedFiles.Add(file.FileName);
                    totalInstallSize += file.Size;
                    continue;
                }

                File.Delete(filePath);
            }

            Path.GetDirectoryName(filePath).CreateFolderIfItDoesNotExist();
            await _s3StorageService.DownloadS3FileToPath(mod, file.FileName, filePath, read => progress.DownloadedSize += (ulong)read);

            if (filePath.GetMd5HashOfFile() == file.Hash)
            {
                progress.DownloadedFiles.Add(file.FileName);
                installedFiles.Add(file.FileName);
                totalInstallSize += file.Size;
            }
            else if (!filePath.ToLower().Contains("changelog") && Path.GetExtension(filePath).ToLower() != ".txt")
            {
                throw new ApiException(502, "Hash did not match, file failed to download: " + file.FileName);
            }
        }

        return (installedFiles, totalInstallSize);
    }

    private async Task<(List<string> installedFiles, ulong totalSize)> DownloadModFromSimpleLink(Mod mod, string modDir, string progressKey)
    {
        var progress = new ModDownloadProgress
        {
            TotalDownloadSize = 0,
            FileList = new List<string>(),
            DownloadedFiles = new List<string>(),
            DownloadedSize = 0,
            Downloaded = false
        };
        _modInstallInfo[progressKey] = progress;

        var link = mod.ModData.SimpleDownloadLink.ParseDownloadLink();
        var client = _httpClientFactory.CreateClient(DownloadClientName);
        using var response = await client.GetAsync(link, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        progress.TotalDownloadSize = (ulong)(response.Content.Headers.ContentLength ?? 0);

        // Prefer the MIME type for the archive extension, fall back to the URL
        var extension = response.Content.Headers.ContentType?.MediaType?.GetFileExtensionFromMimeType()
                        ?? Path.GetExtension(new Uri(link).AbsolutePath);
        var archivePath = Path.Combine(modDir, Path.GetFileName(modDir) + extension);

        // Stream straight to disk; large mods must not be buffered in memory
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
        {
            throw new ApiException(502, "Downloaded mod archive could not be extracted: " + Path.GetFileName(archivePath));
        }

        var allFiles = FileSystemExtensions.GetAllFilesRecursively(modDir);
        // Stored relative to the mod folder so install can project them into the game folder
        var installedFiles = allFiles.Select(f => Path.GetRelativePath(modDir, f)).ToList();
        var totalInstallSize = (ulong)FileSystemExtensions.GetTotalSizeInBytes(allFiles);

        progress.DownloadedFiles = installedFiles;
        progress.DownloadedSize = totalInstallSize;
        if (progress.TotalDownloadSize == 0)
        {
            progress.TotalDownloadSize = totalInstallSize;
        }

        return (installedFiles, totalInstallSize);
    }

    public ModDownloadProgress GetModDownloadProgress(string modName)
    {
        var key = modName.StandardModName();
        if (_modInstallInfo.TryGetValue(key, out var progress))
        {
            if (progress.Downloaded)
            {
                // Final progress is handed out once, then the entry is cleared
                _modInstallInfo.TryRemove(key, out _);
            }

            return progress;
        }

        return new ModDownloadProgress
        {
            DownloadedFiles = null,
            FileList = null,
            DownloadedSize = 0,
            TotalDownloadSize = 0,
            Downloaded = false
        };
    }

    // -------------------------------------------------- install / uninstall

    public void InstallMod(string modName)
    {
        var game = SelectedGame;
        lock (_lock)
        {
            var modList = GetModList(game);
            var mod = FindMod(game, modName);
            if (!mod.Downloaded || mod.DownloadedFiles == null)
            {
                throw new ApiException(409, "The mod must be downloaded before it can be installed.");
            }

            // There can only be one installed mod at a time, to ensure mod compatibility
            if (modList.Any(x => x.Installed))
            {
                throw new ApiException(409, "There is already another mod installed. Please uninstall it first.");
            }

            EnsureModdedLauncherInstalled(game);

            var gameFolder = GetGameDir(game);
            var options = _optionsService.GetOptions();
            foreach (var modFile in mod.DownloadedFiles)
            {
                var modFileName = modFile.FixModFileName();
                var gameFilePath = Path.Combine(gameFolder, modFileName);
                if (File.Exists(gameFilePath))
                {
                    BackupOriginalGameFile(game, modFileName);
                }

                var sourceFile = Path.Combine(mod.ModDir, modFile);
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

            // Verify every file landed; roll back on any miss
            foreach (var modFile in mod.DownloadedFiles)
            {
                var gameFilePath = Path.Combine(gameFolder, modFile.FixModFileName());
                if (!File.Exists(gameFilePath))
                {
                    UninstallCore(game, mod);
                    UpdateModListFile(game);
                    throw new ApiException(500, "Failed to install mod. File: " + gameFilePath + " does not exist");
                }
            }

            mod.Installed = true;
            UpdateModListFile(game);
        }
    }

    public void UninstallMod(string modName)
    {
        var game = SelectedGame;
        lock (_lock)
        {
            var mod = FindMod(game, modName);
            UninstallCore(game, mod);
            UpdateModListFile(game);
        }
    }

    private void UninstallCore(GameType game, Mod mod)
    {
        var gameFolder = GetGameDir(game);

        foreach (var modFile in mod.DownloadedFiles ?? new List<string>())
        {
            var modFileName = modFile.FixModFileName();
            var gameFilePath = Path.Combine(gameFolder, modFileName);
            // Restore the displaced original game file; a file without a backup was
            // purely a mod file and is deleted instead
            if (!RestoreOriginalGameFile(game, modFileName) && File.Exists(gameFilePath))
            {
                File.Delete(gameFilePath);
            }
        }

        mod.Installed = false;
    }

    public void DeleteMod(string modName)
    {
        var game = SelectedGame;
        lock (_lock)
        {
            var mod = FindMod(game, modName);
            if (mod.Installed)
            {
                throw new ApiException(409, "Cannot delete the files of an installed mod. Uninstall it first.");
            }

            DeleteModFilesCore(mod);
            UpdateModListFile(game);
        }
    }

    private static void DeleteModFilesCore(Mod mod)
    {
        if (!string.IsNullOrEmpty(mod.ModDir))
        {
            try
            {
                Directory.Delete(mod.ModDir, true);
            }
            catch (DirectoryNotFoundException)
            {
                // Already gone
            }
        }

        mod.Downloaded = false;
        mod.DownloadedVersion = null;
        mod.DownloadedFiles = null;
        mod.TotalSize = 0;
        mod.ModDir = null;
    }

    // ------------------------------------------------------ backup / restore

    /// <summary>
    /// Moves an original game file into the per-game backup folder, keeping the
    /// folder structure. <paramref name="relativePath"/> is relative to the game root.
    /// </summary>
    public void BackupOriginalGameFile(GameType game, string relativePath)
    {
        var gameFile = Path.Combine(GetGameDir(game), relativePath);
        if (!File.Exists(gameFile))
        {
            return;
        }

        var destination = Path.Combine(GetBackupFolder(game), relativePath);
        Path.GetDirectoryName(destination).CreateFolderIfItDoesNotExist();
        File.Move(gameFile, destination, true);
    }

    /// <summary>
    /// Restores a backed-up original game file. Returns false when no backup exists.
    /// </summary>
    public bool RestoreOriginalGameFile(GameType game, string relativePath)
    {
        var currentGameFile = Path.Combine(GetGameDir(game), relativePath);
        var backupFile = Path.Combine(GetBackupFolder(game), relativePath);
        if (!File.Exists(backupFile) && game == GameType.ZH)
        {
            // Backups from before per-game folders lived directly in OriginalGameFiles/
            backupFile = Path.Combine(GetModStorageDir(), StorageNames.OriginalGameFilesDir, relativePath);
        }

        if (!File.Exists(backupFile))
        {
            return false;
        }

        Path.GetDirectoryName(currentGameFile).CreateFolderIfItDoesNotExist();
        File.Move(backupFile, currentGameFile, true);
        return true;
    }

    // ------------------------------------------------------- modded launcher

    public bool CheckModdedLauncherInstalled(GameType game)
    {
        var gameExe = Path.Combine(GetGameDir(game), GameFiles.GameExe);
        var moddedExeFile = Path.Combine(GetModStorageDir(), StorageNames.ModdedLauncherDir, GameFiles.ModdedExe);
        return File.Exists(gameExe) && File.Exists(moddedExeFile) && gameExe.GetMd5HashOfFile() == moddedExeFile.GetMd5HashOfFile();
    }

    public void EnsureModdedLauncherInstalled(GameType game)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && _steamService.CheckGameRunning())
        {
            throw new ApiException(409, "Cannot install the modded launcher while the game is running. Please close the game first.");
        }

        if (CheckModdedLauncherInstalled(game))
        {
            return;
        }

        DownloadModdedExe();
        InstallModdedLauncher(game);
    }

    private void InstallModdedLauncher(GameType game)
    {
        var gameExe = Path.Combine(GetGameDir(game), GameFiles.GameExe);
        var moddedExeFile = Path.Combine(GetModStorageDir(), StorageNames.ModdedLauncherDir, GameFiles.ModdedExe);
        if (!File.Exists(moddedExeFile))
        {
            throw new ApiException(500, "The modded launcher could not be downloaded. Mods will not work without it.");
        }

        if (File.Exists(gameExe))
        {
            BackupOriginalGameFile(game, GameFiles.GameExe);
        }

        switch (_optionsService.GetOptions().InstallMethod)
        {
            case InstallMethod.CopyFiles:
                File.Copy(moddedExeFile, gameExe, true);
                break;
            case InstallMethod.SymLink:
                SymLinkService.CreateSymbolicLink(gameExe, moddedExeFile);
                break;
        }
    }

    private void DownloadModdedExe()
    {
        var moddedExeFolder = Path.Combine(GetModStorageDir(), StorageNames.ModdedLauncherDir);
        moddedExeFolder.CreateFolderIfItDoesNotExist();
        var moddedExeFile = Path.Combine(moddedExeFolder, GameFiles.ModdedExe);
        if (File.Exists(moddedExeFile) && (string.IsNullOrEmpty(ModdedGameExeHash) || moddedExeFile.VerifyFileHash(ModdedGameExeHash)))
        {
            return;
        }

        DownloadToFile(ModdedGameExeDownloadLink, moddedExeFile);

        if (!string.IsNullOrEmpty(ModdedGameExeHash) && !moddedExeFile.VerifyFileHash(ModdedGameExeHash))
        {
            File.Delete(moddedExeFile);
            throw new ApiException(502, "The downloaded modded launcher failed its integrity check.");
        }
    }

    // ---------------------------------------------------------------- GenTool

    public InstallationStatus GetInstallationStatus()
    {
        var game = SelectedGame;
        bool moddedLauncher;
        try
        {
            moddedLauncher = CheckModdedLauncherInstalled(game);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not determine modded launcher status");
            moddedLauncher = false;
        }

        bool genTool;
        try
        {
            genTool = CheckGenToolInstalled(game);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not determine GenTool status");
            genTool = false;
        }

        return new InstallationStatus
        {
            ModdedLauncher = moddedLauncher,
            GenTool = genTool
        };
    }

    public bool CheckGenToolInstalled(GameType game)
    {
        var gentoolDll = Path.Combine(GetGameDir(game), GameFiles.GenToolDll);
        return File.Exists(gentoolDll) && gentoolDll.VerifyFileHash(GenToolDllHash);
    }

    public bool EnsureGenToolInstalled()
    {
        var game = SelectedGame;
        if (CheckGenToolInstalled(game))
        {
            return true;
        }

        BackupOriginalGameFile(game, GameFiles.GenToolDll);
        DownloadGenTool();
        InstallGenTool(game);
        return true;
    }

    private void InstallGenTool(GameType game)
    {
        var gameDir = GetGameDir(game);
        var genToolFolder = Path.Combine(GetModStorageDir(), StorageNames.GenToolDir);
        var d3d8Dll = FileSystemExtensions.FindFileRecursively(genToolFolder, GameFiles.GenToolDll);
        if (d3d8Dll == null || !File.Exists(d3d8Dll))
        {
            throw new ApiException(500, "GenTool could not be downloaded.");
        }

        switch (_optionsService.GetOptions().InstallMethod)
        {
            case InstallMethod.CopyFiles:
                File.Copy(d3d8Dll, Path.Combine(gameDir, GameFiles.GenToolDll), true);
                break;
            case InstallMethod.SymLink:
                SymLinkService.CreateSymbolicLink(Path.Combine(gameDir, GameFiles.GenToolDll), d3d8Dll);
                break;
        }
    }

    private void DownloadGenTool()
    {
        var genToolFolder = Path.Combine(GetModStorageDir(), StorageNames.GenToolDir);
        genToolFolder.CreateFolderIfItDoesNotExist();
        if (File.Exists(Path.Combine(genToolFolder, GameFiles.GenToolDll)))
        {
            return;
        }

        var zipPath = Path.Combine(genToolFolder, StorageNames.GenToolArchive);
        DownloadToFile(GentoolDownloadLink, zipPath);
        ZipFile.ExtractToDirectory(zipPath, genToolFolder);
        File.Delete(zipPath);
    }

    // ---------------------------------------------------------------- download helper

    /// <summary>
    /// Downloads a URL straight to a file using the synchronous <see cref="HttpClient.Send"/>
    /// API (no sync-over-async), streaming the body to disk.
    /// </summary>
    private void DownloadToFile(string url, string destinationPath)
    {
        var client = _httpClientFactory.CreateClient(DownloadClientName);
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = client.Send(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        using var source = response.Content.ReadAsStream();
        using var fileStream = File.Create(destinationPath);
        source.CopyTo(fileStream);
    }
}
