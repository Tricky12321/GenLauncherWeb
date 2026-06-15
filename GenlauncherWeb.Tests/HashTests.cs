using FluentAssertions;
using GenLauncherWeb;
using NUnit.Framework;

namespace GenlauncherWeb.Tests;

/// <summary>
/// Tests the file hashing helpers, including the length-based algorithm selection that
/// lets integrity verification be upgraded from MD5 to SHA-256 just by supplying a
/// SHA-256 digest.
/// </summary>
[TestFixture]
public class HashTests
{
    private string _tempDir = null!;

    // Well-known digests of the empty file.
    private const string EmptyMd5 = "d41d8cd98f00b204e9800998ecf8427e";
    private const string EmptySha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"GL_Hash_{Guid.NewGuid():N}"[..24]);
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_tempDir, recursive: true);

    private string WriteFile(byte[] content)
    {
        var path = Path.Combine(_tempDir, "data.bin");
        File.WriteAllBytes(path, content);
        return path;
    }

    [Test]
    public void GetMd5HashOfFile_EmptyFile_MatchesKnownDigest()
    {
        WriteFile([]).GetMd5HashOfFile().Should().Be(EmptyMd5);
    }

    [Test]
    public void GetSha256HashOfFile_EmptyFile_MatchesKnownDigest()
    {
        WriteFile([]).GetSha256HashOfFile().Should().Be(EmptySha256);
    }

    [Test]
    public void VerifyFileHash_CorrectSha256_ReturnsTrue()
    {
        var path = WriteFile([1, 2, 3, 4]);
        path.VerifyFileHash(path.GetSha256HashOfFile()).Should().BeTrue();
    }

    [Test]
    public void VerifyFileHash_CorrectMd5_ReturnsTrue()
    {
        var path = WriteFile([1, 2, 3, 4]);
        path.VerifyFileHash(path.GetMd5HashOfFile()).Should().BeTrue();
    }

    [Test]
    public void VerifyFileHash_PicksAlgorithmByDigestLength()
    {
        var path = WriteFile([]);

        // 64 hex chars => SHA-256 path
        path.VerifyFileHash(EmptySha256).Should().BeTrue();
        // 32 hex chars => MD5 path
        path.VerifyFileHash(EmptyMd5).Should().BeTrue();
    }

    [Test]
    public void VerifyFileHash_WrongHash_ReturnsFalse()
    {
        var path = WriteFile([9, 9, 9]);
        path.VerifyFileHash(new string('a', 64)).Should().BeFalse();
        path.VerifyFileHash(new string('a', 32)).Should().BeFalse();
    }

    [Test]
    public void VerifyFileHash_IsCaseInsensitive()
    {
        var path = WriteFile([]);
        path.VerifyFileHash(EmptySha256.ToUpperInvariant()).Should().BeTrue();
    }
}
