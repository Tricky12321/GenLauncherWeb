using GenLauncherWeb.Models;

namespace GenLauncherWeb;

/// <summary>
/// Domain helpers for mods and patches.
/// </summary>
public static class ModExtensions
{
    /// <summary>True when the mod is hosted on S3-style storage rather than a simple link.</summary>
    public static bool HasS3Storage(this Mod mod)
        => HasS3Storage(mod.ModData);

    /// <summary>True when the metadata points at S3-style storage.</summary>
    public static bool HasS3Storage(this ModData modData)
        => modData != null
           && !string.IsNullOrEmpty(modData.S3HostLink)
           && !string.IsNullOrEmpty(modData.S3FolderName)
           && !string.IsNullOrEmpty(modData.S3BucketName);
}
