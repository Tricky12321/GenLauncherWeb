using System.Text;
using System.Text.RegularExpressions;

namespace GenLauncherWeb;

/// <summary>
/// String normalization helpers for mod names, file names and shell paths.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Produces a file-system-safe folder name from a mod name: ASCII letters only,
    /// spaces collapsed to underscores.
    /// </summary>
    public static string CleanString(this string input)
    {
        // Remove non-ASCII characters
        string asciiOnly = Regex.Replace(input, @"[^\x00-\x7F]", "");

        // Remove special characters except a-z and A-Z, replace spaces with underscores
        string cleaned = Regex.Replace(asciiOnly, @"[^a-zA-Z\s]", "");
        cleaned = Regex.Replace(cleaned, @"\s+", "_");

        return cleaned;
    }

    /// <summary>
    /// A stable key for a mod/patch: lowercase alphanumerics only. Used to key
    /// download-progress entries so the same name always maps to the same slot.
    /// </summary>
    public static string StandardModName(this string modName)
    {
        return Regex.Replace(modName, @"[^a-zA-Z0-9]", "").ToLower();
    }

    /// <summary>
    /// Some mods ship <c>.gib</c> files that must be installed as <c>.big</c>.
    /// </summary>
    public static string FixModFileName(this string modName)
    {
        if (modName.EndsWith(".gib"))
        {
            modName = modName.Replace(".gib", ".big");
        }

        return modName;
    }

    /// <summary>
    /// Single-quotes a path for safe use in a POSIX shell command.
    /// </summary>
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
}
