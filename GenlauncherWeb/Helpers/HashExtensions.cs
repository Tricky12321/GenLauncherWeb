using System;
using System.IO;
using System.Security.Cryptography;

namespace GenLauncherWeb;

/// <summary>
/// File-content hashing. SHA-256 is used to verify launcher-supplied executables
/// (the modded launcher, GenTool); MD5 remains only for comparing against S3 ETags,
/// whose algorithm is dictated by the remote.
/// </summary>
public static class HashExtensions
{
    /// <summary>MD5 hex digest. Only for S3 ETag comparison — not for trust decisions.</summary>
    public static string GetMd5HashOfFile(this string path)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(md5.ComputeHash(stream)).ToLowerInvariant();
    }

    /// <summary>SHA-256 hex digest of a file.</summary>
    public static string GetSha256HashOfFile(this string path)
    {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(sha.ComputeHash(stream)).ToLowerInvariant();
    }

    /// <summary>
    /// Verifies a file against an expected hex digest, picking the algorithm from the
    /// digest length (64 = SHA-256, 32 = MD5). This lets verification be upgraded to
    /// SHA-256 simply by supplying a SHA-256 hash in configuration, while older MD5
    /// hashes keep working until they are rotated.
    /// </summary>
    public static bool VerifyFileHash(this string path, string expectedHash)
    {
        if (string.IsNullOrWhiteSpace(expectedHash) || !File.Exists(path))
        {
            return false;
        }

        var actual = expectedHash.Length == 64 ? path.GetSha256HashOfFile() : path.GetMd5HashOfFile();
        return string.Equals(actual, expectedHash, StringComparison.OrdinalIgnoreCase);
    }
}
