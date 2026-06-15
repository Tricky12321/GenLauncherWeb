using System.Collections.Generic;
using System.IO;

namespace GenLauncherWeb;

/// <summary>
/// File-system helpers: folder creation, symlink detection and recursive enumeration.
/// </summary>
public static class FileSystemExtensions
{
    /// <summary>Creates the folder if missing. Returns true when it was created.</summary>
    public static bool CreateFolderIfItDoesNotExist(this string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            return true;
        }

        return false;
    }

    /// <summary>True when the path is a reparse point (symbolic link / junction).</summary>
    public static bool IsSymbolicLink(string path)
    {
        var fileInfo = new FileInfo(path);
        return fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
    }

    /// <summary>All files under <paramref name="path"/>, recursively.</summary>
    public static List<string> GetAllFilesRecursively(string path)
    {
        return new List<string>(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));
    }

    /// <summary>
    /// Sum of the sizes of the given files. Files that no longer exist are skipped so a
    /// stale list does not break size accounting.
    /// </summary>
    public static long GetTotalSizeInBytes(List<string> filePaths)
    {
        long totalSize = 0;
        foreach (var filePath in filePaths)
        {
            if (File.Exists(filePath))
            {
                totalSize += new FileInfo(filePath).Length;
            }
        }

        return totalSize;
    }

    /// <summary>First file matching <paramref name="fileName"/> under the folder, or null.</summary>
    public static string FindFileRecursively(string folderPath, string fileName)
    {
        foreach (var file in Directory.GetFiles(folderPath))
        {
            if (Path.GetFileName(file).Equals(fileName, System.StringComparison.OrdinalIgnoreCase))
            {
                return file;
            }
        }

        foreach (var directory in Directory.GetDirectories(folderPath))
        {
            var foundFile = FindFileRecursively(directory, fileName);
            if (foundFile != null)
            {
                return foundFile;
            }
        }

        return null;
    }
}
