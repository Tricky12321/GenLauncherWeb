using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;

namespace GenLauncherWeb;

/// <summary>
/// Extracts downloaded mod/patch archives. Every format is unpacked through
/// SharpCompress (cross-platform, no native dependency) except ZIP, which uses the BCL.
/// All paths are validated to stay inside the destination folder (zip-slip guard).
/// </summary>
public static class ArchiveExtractor
{
    private static readonly Dictionary<string, string> ArchiveMimeTypeMappings =
        new(StringComparer.InvariantCultureIgnoreCase)
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

    /// <summary>Maps an archive MIME type to a file extension, or null when unknown.</summary>
    public static string GetFileExtensionFromMimeType(this string mimeType)
    {
        return ArchiveMimeTypeMappings.TryGetValue(mimeType, out var extension) ? extension : null;
    }

    /// <summary>
    /// Extracts an archive into its own folder, deleting the archive on success.
    /// Returns false for unrecognized extensions (and leaves the file intact).
    /// </summary>
    public static bool ExtractFile(this string fileName)
    {
        var fileFolder = Path.GetDirectoryName(fileName);
        var fileExtension = Path.GetExtension(fileName);
        switch (fileExtension)
        {
            case ".zip":
                return ExtractZipFile(fileName, fileFolder, true);
            case ".7z":
                return ExtractWithSharpCompress(SevenZipArchive.OpenArchive(fileName, null), fileName, fileFolder, true);
            case ".rar":
                return ExtractWithSharpCompress(RarArchive.OpenArchive(fileName, null), fileName, fileFolder, true);
            default:
                return false;
        }
    }

    /// <summary>
    /// Resolves an archive entry's path against the destination and rejects anything
    /// that would escape it (e.g. <c>../../etc</c>). Returns the full, safe path.
    /// </summary>
    private static string SafeDestinationPath(string destination, string entryPath)
    {
        var destinationRoot = Path.GetFullPath(destination);
        var fullPath = Path.GetFullPath(Path.Combine(destinationRoot, entryPath));

        var rootWithSeparator = destinationRoot.EndsWith(Path.DirectorySeparatorChar)
            ? destinationRoot
            : destinationRoot + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            throw new IOException($"Archive entry '{entryPath}' would extract outside the destination folder.");
        }

        return fullPath;
    }

    private static bool ExtractWithSharpCompress(IArchive archive, string fileName, string destination, bool deleteFile)
    {
        using (archive)
        {
            foreach (var entry in archive.Entries)
            {
                if (entry.IsDirectory)
                {
                    continue;
                }

                var filePath = SafeDestinationPath(destination, entry.Key);
                Path.GetDirectoryName(filePath).CreateFolderIfItDoesNotExist();
                entry.WriteToFile(filePath, new ExtractionOptions { Overwrite = true });

                if (!File.Exists(filePath))
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
        using (var archive = ZipFile.OpenRead(fileName))
        {
            foreach (var entry in archive.Entries)
            {
                // Directory entries have an empty name
                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                var filePath = SafeDestinationPath(destination, entry.FullName);
                Path.GetDirectoryName(filePath).CreateFolderIfItDoesNotExist();
                entry.ExtractToFile(filePath, true);
            }
        }

        if (deleteFile)
        {
            File.Delete(fileName);
        }

        return true;
    }
}
