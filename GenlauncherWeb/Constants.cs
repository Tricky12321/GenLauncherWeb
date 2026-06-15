using GenLauncherWeb.Enums;

namespace GenLauncherWeb;

/// <summary>
/// Well-known file names that live in the game install folder. Both games share the
/// same executable and GenTool DLL names.
/// </summary>
public static class GameFiles
{
    /// <summary>The game executable; replaced by the modded launcher when mods are installed.</summary>
    public const string GameExe = "Generals.exe";

    /// <summary>GenTool ships as a d3d8 proxy DLL.</summary>
    public const string GenToolDll = "d3d8.dll";

    /// <summary>The downloaded modded launcher executable.</summary>
    public const string ModdedExe = "modded.exe";
}

/// <summary>
/// Names of the launcher-managed folders and bookkeeping files inside mod storage
/// (<c>&lt;steamapps/common&gt;/GenLauncherMods</c>) and the per-user config folder.
/// </summary>
public static class StorageNames
{
    // Sub-folders of the mod storage directory
    public const string ModdedLauncherDir = "ModdedLauncher";
    public const string GenToolDir = "GenTool";
    public const string PatchesDir = "Patches";

    /// <summary>Backup root for displaced original game files (per game beneath it).</summary>
    public const string OriginalGameFilesDir = "OriginalGameFiles";

    public const string GenToolArchive = "gentool.zip";

    // Per-game bookkeeping files
    public const string LegacyModListFile = "genlauncher_modlist.json";
    public const string OptionsFile = "genlauncher_options.json";

    public static string ModListFile(GameType game)
        => game == GameType.Gen ? "genlauncher_modlist_gen.json" : "genlauncher_modlist_zh.json";

    public static string PatchListFile(GameType game)
        => game == GameType.Gen ? "genlauncher_patches_gen.json" : "genlauncher_patches_zh.json";
}
