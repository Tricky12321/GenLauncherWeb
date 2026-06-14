using System.Text;
using FluentAssertions;
using GenLauncherWeb;
using GenlauncherWeb.Tests.Helpers;
using NUnit.Framework;

namespace GenlauncherWeb.Tests;

/// <summary>
/// Simulates the HTTP file-download behaviour used by ModService and PatchService
/// using an in-process HTTP server — no real network traffic needed.
/// </summary>
[TestFixture]
public class DownloadTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"GL_DL_{Guid.NewGuid():N}"[..24]);
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_tempDir, recursive: true);

    // -------------------------------------------------------------- helpers

    /// <summary>
    /// Mirrors the streaming-download pattern used in ModService / PatchService.
    /// </summary>
    private static async Task DownloadFileAsync(
        string url, string destPath, Action<long>? onProgress = null)
    {
        using var client = new HttpClient();
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using var contentStream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = File.Create(destPath);

        var buffer = new byte[8192];
        long downloaded = 0;
        int read;
        while ((read = await contentStream.ReadAsync(buffer)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read));
            downloaded += read;
            onProgress?.Invoke(downloaded);
        }
    }

    // -------------------------------------------------------------- tests

    [Test]
    public async Task Download_WritesCorrectBytesToFile()
    {
        var expected = Encoding.UTF8.GetBytes("GenLauncher payload " + new string('A', 2048));
        using var server = new LocalHttpServer(expected);
        var dest = Path.Combine(_tempDir, "file.bin");

        await DownloadFileAsync(server.Url, dest);

        File.Exists(dest).Should().BeTrue();
        File.ReadAllBytes(dest).Should().Equal(expected);
    }

    [Test]
    public async Task Download_ReportsProgressIncrementally()
    {
        var content = new byte[128 * 1024]; // 128 KB — many read chunks
        new Random(1).NextBytes(content);

        using var server = new LocalHttpServer(content, chunkDelay: 1);
        var dest = Path.Combine(_tempDir, "progress.bin");
        var samples = new List<long>();

        await DownloadFileAsync(server.Url, dest, downloaded => samples.Add(downloaded));

        samples.Should().HaveCountGreaterThan(1, "a chunk-based read should yield multiple progress callbacks");
        samples.Last().Should().Be(content.Length);
        samples.Should().BeInAscendingOrder();
    }

    [Test]
    public async Task Download_WithContentLengthHeader_ReadsLengthCorrectly()
    {
        var content = new byte[4096];
        using var server = new LocalHttpServer(content, setContentLength: true);

        using var client = new HttpClient();
        using var response = await client.GetAsync(server.Url, HttpCompletionOption.ResponseHeadersRead);

        response.Content.Headers.ContentLength.Should().Be(content.Length);
    }

    [Test]
    public async Task Download_WithoutContentLengthHeader_StillCompletesSuccessfully()
    {
        var content = new byte[4096];
        new Random(2).NextBytes(content);

        using var server = new LocalHttpServer(content, setContentLength: false);
        var dest = Path.Combine(_tempDir, "no_len.bin");

        await DownloadFileAsync(server.Url, dest);

        new FileInfo(dest).Length.Should().Be(content.Length);
    }

    [Test]
    public void DownloadYaml_ReturnsServerResponseAsString()
    {
        const string yaml = "Name: TestPatch\nVersion: 1.2.3\nS3HostLink: \"\"\n";
        var content = Encoding.UTF8.GetBytes(yaml);

        using var server = new LocalHttpServer(content, contentType: "text/plain");

        var result = Extensions.DownloadYaml(server.Url);

        result.Should().Be(yaml);
    }
}
