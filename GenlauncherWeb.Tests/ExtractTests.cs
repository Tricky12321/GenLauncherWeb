using System.Diagnostics;
using System.IO.Compression;
using FluentAssertions;
using GenLauncherWeb;
using NUnit.Framework;

namespace GenlauncherWeb.Tests;

/// <summary>
/// Tests for archive extraction (ZIP, 7z) and the file utility helpers in Extensions.
/// Archives are built in-memory so no pre-built fixtures are needed.
/// </summary>
[TestFixture]
public class ExtractTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"GL_Ext_{Guid.NewGuid():N}"[..24]);
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_tempDir, recursive: true);

    // -------------------------------------------------------------- helpers

    private string MakeSourceDir(string name, Dictionary<string, string> files)
    {
        var dir = Path.Combine(_tempDir, name);
        Directory.CreateDirectory(dir);
        foreach (var (rel, content) in files)
        {
            var full = Path.Combine(dir, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, content);
        }
        return dir;
    }

    // -------------------------------------------------------------- ZIP

    [Test]
    public void ExtractZip_ExtractsAllFilesAndDeletesArchive()
    {
        var sourceDir = MakeSourceDir("zip_src", new()
        {
            ["data.big"]       = "mod binary content",
            ["sub/shader.fx"]  = "shader source"
        });

        var destDir = Path.Combine(_tempDir, "zip_out");
        Directory.CreateDirectory(destDir);

        var zipPath = Path.Combine(destDir, "mod.zip");
        ZipFile.CreateFromDirectory(sourceDir, zipPath);

        var result = zipPath.ExtractFile();

        result.Should().BeTrue();
        File.Exists(zipPath).Should().BeFalse("archive is deleted after successful extraction");
        File.ReadAllText(Path.Combine(destDir, "data.big")).Should().Be("mod binary content");
        File.ReadAllText(Path.Combine(destDir, "sub", "shader.fx")).Should().Be("shader source");
    }

    [Test]
    public void ExtractZip_LargeFile_ContentIsPreserved()
    {
        var content = new string('Z', 64 * 1024); // 64 KB
        var sourceDir = MakeSourceDir("zip_big", new() { ["large.dat"] = content });

        var destDir = Path.Combine(_tempDir, "zip_big_out");
        Directory.CreateDirectory(destDir);
        var zipPath = Path.Combine(destDir, "large.zip");
        ZipFile.CreateFromDirectory(sourceDir, zipPath);

        zipPath.ExtractFile();

        File.ReadAllText(Path.Combine(destDir, "large.dat")).Should().Be(content);
    }

    // -------------------------------------------------------------- 7z

    private static string? Find7zCli() =>
        new[] { "7z", "7za", "7zr" }
            .Select(cmd => { try { return new FileInfo(Environment.GetEnvironmentVariable("PATH")!
                .Split(':').Select(d => Path.Combine(d, cmd)).FirstOrDefault(File.Exists) ?? ""); } catch { return null; } })
            .FirstOrDefault(p => p is { Exists: true })?.FullName;

    private void Create7zArchive(string sourceDir, string destArchive)
    {
        var cli = Find7zCli();
        if (cli == null)
            Assert.Ignore("7z CLI not found — skipping 7z creation tests.");

        var result = Process.Start(new ProcessStartInfo(cli, $"a \"{destArchive}\" \"{sourceDir}\"/*")
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        })!;
        result.WaitForExit();
        if (result.ExitCode != 0)
            Assert.Fail("7z CLI failed to create archive.");
    }

    [Test]
    [Platform(Include = "Win")]
    public void Extract7z_ExtractsFilesAndDeletesArchive()
    {
        var sourceDir = MakeSourceDir("7z_src", new()
        {
            ["patch.big"] = "seven-zip patch content"
        });

        var destDir = Path.Combine(_tempDir, "7z_out");
        Directory.CreateDirectory(destDir);

        var archivePath = Path.Combine(destDir, "patch.7z");
        Create7zArchive(sourceDir, archivePath);

        var result = archivePath.ExtractFile();

        result.Should().BeTrue();
        File.Exists(archivePath).Should().BeFalse("archive is deleted after successful extraction");
        File.ReadAllText(Path.Combine(destDir, "patch.big")).Should().Be("seven-zip patch content");
    }

    [Test]
    [Platform(Include = "Win")]
    public void Extract7z_MultipleFiles_AllExtracted()
    {
        var sourceDir = MakeSourceDir("7z_multi", new()
        {
            ["a.big"]      = "file a",
            ["b.big"]      = "file b",
            ["sub/c.big"]  = "file c"
        });

        var destDir = Path.Combine(_tempDir, "7z_multi_out");
        Directory.CreateDirectory(destDir);

        var archivePath = Path.Combine(destDir, "multi.7z");
        Create7zArchive(sourceDir, archivePath);

        archivePath.ExtractFile();

        File.ReadAllText(Path.Combine(destDir, "a.big")).Should().Be("file a");
        File.ReadAllText(Path.Combine(destDir, "b.big")).Should().Be("file b");
        File.ReadAllText(Path.Combine(destDir, "sub", "c.big")).Should().Be("file c");
    }

    // -------------------------------------------------------------- extension routing

    [Test]
    public void ExtractFile_UnknownExtension_ReturnsFalseAndLeavesFileIntact()
    {
        var file = Path.Combine(_tempDir, "mystery.xyz");
        File.WriteAllBytes(file, [0x00, 0x01, 0x02]);

        var result = file.ExtractFile();

        result.Should().BeFalse();
        File.Exists(file).Should().BeTrue("unrecognised extension must not delete the file");
    }

    // -------------------------------------------------------------- file utilities

    [Test]
    public void GetAllFilesRecursively_FindsAllNestedFiles()
    {
        var dir = MakeSourceDir("recursive", new()
        {
            ["root.txt"]       = "r",
            ["sub/a.txt"]      = "a",
            ["sub/deep/b.txt"] = "b"
        });

        var files = Extensions.GetAllFilesRecursively(dir);

        files.Should().HaveCount(3);
        files.Should().OnlyContain(f => File.Exists(f));
    }

    [Test]
    public void GetTotalSizeInBytes_SumsAllFileSizes()
    {
        var dir = MakeSourceDir("sizes", new()
        {
            ["f100.dat"] = new string('x', 100),
            ["f200.dat"] = new string('y', 200)
        });

        var total = Extensions.GetTotalSizeInBytes(
        [
            Path.Combine(dir, "f100.dat"),
            Path.Combine(dir, "f200.dat")
        ]);

        total.Should().Be(300);
    }

    [Test]
    public void GetTotalSizeInBytes_MissingFileIsSkippedGracefully()
    {
        var existing = Path.Combine(_tempDir, "exists.dat");
        File.WriteAllBytes(existing, new byte[64]);

        var total = Extensions.GetTotalSizeInBytes(
        [
            existing,
            Path.Combine(_tempDir, "does_not_exist.dat")
        ]);

        total.Should().Be(64);
    }
}
