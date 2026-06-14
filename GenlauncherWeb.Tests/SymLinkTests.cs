using FluentAssertions;
using GenLauncherWeb;
using GenLauncherWeb.Services;
using NUnit.Framework;

namespace GenlauncherWeb.Tests;

/// <summary>
/// Tests for symbolic link creation and detection via SymLinkService and Extensions.
/// Tests that require symlink OS support are skipped automatically when unavailable
/// (e.g. Windows without Developer Mode or Administrator rights).
/// </summary>
[TestFixture]
public class SymLinkTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"GL_Sym_{Guid.NewGuid():N}"[..24]);
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_tempDir, recursive: true);

    // -------------------------------------------------------------- support detection

    [Test]
    public void IsSymlinksSupported_DoesNotThrow()
    {
        // Verifies the check itself runs cleanly on every platform
        var act = () => SymLinkService.IsSymlinksSupported();
        act.Should().NotThrow();
    }

    // -------------------------------------------------------------- IsSymbolicLink

    [Test]
    public void IsSymbolicLink_RegularFile_ReturnsFalse()
    {
        var file = Path.Combine(_tempDir, "plain.txt");
        File.WriteAllText(file, "content");

        Extensions.IsSymbolicLink(file).Should().BeFalse();
    }

    [Test]
    public void IsSymbolicLink_Symlink_ReturnsTrue()
    {
        if (!SymLinkService.IsSymlinksSupported())
            Assert.Ignore("Symlinks not supported on this system — skipping.");

        var source = Path.Combine(_tempDir, "src.txt");
        var link   = Path.Combine(_tempDir, "lnk.txt");
        File.WriteAllText(source, "x");
        File.CreateSymbolicLink(link, source);

        Extensions.IsSymbolicLink(link).Should().BeTrue();
    }

    // -------------------------------------------------------------- CreateSymbolicLink

    [Test]
    public void CreateSymbolicLink_CreatesReadableLink()
    {
        if (!SymLinkService.IsSymlinksSupported())
            Assert.Ignore("Symlinks not supported on this system — skipping.");

        var source = Path.Combine(_tempDir, "source.big");
        var link   = Path.Combine(_tempDir, "link.big");
        File.WriteAllText(source, "game mod content");

        var result = SymLinkService.CreateSymbolicLink(link, source);

        result.Should().BeTrue();
        File.Exists(link).Should().BeTrue();
        File.ReadAllText(link).Should().Be("game mod content");
        Extensions.IsSymbolicLink(link).Should().BeTrue();
    }

    [Test]
    public void CreateSymbolicLink_OverwritesExistingLink()
    {
        if (!SymLinkService.IsSymlinksSupported())
            Assert.Ignore("Symlinks not supported on this system — skipping.");

        var src1 = Path.Combine(_tempDir, "v1.big");
        var src2 = Path.Combine(_tempDir, "v2.big");
        var link = Path.Combine(_tempDir, "mod.big");
        File.WriteAllText(src1, "version one");
        File.WriteAllText(src2, "version two");

        SymLinkService.CreateSymbolicLink(link, src1);
        File.ReadAllText(link).Should().Be("version one");

        SymLinkService.CreateSymbolicLink(link, src2);
        File.ReadAllText(link).Should().Be("version two");
    }

    [Test]
    public void CreateSymbolicLink_LinkedFileContentsMatchSource()
    {
        if (!SymLinkService.IsSymlinksSupported())
            Assert.Ignore("Symlinks not supported on this system — skipping.");

        var content = new byte[4096];
        new Random(99).NextBytes(content);

        var source = Path.Combine(_tempDir, "binary.dat");
        var link   = Path.Combine(_tempDir, "link.dat");
        File.WriteAllBytes(source, content);

        SymLinkService.CreateSymbolicLink(link, source);

        File.ReadAllBytes(link).Should().Equal(content);
    }

    [Test]
    public void CreateSymbolicLink_LinkIsCreatedInSubdirectory()
    {
        if (!SymLinkService.IsSymlinksSupported())
            Assert.Ignore("Symlinks not supported on this system — skipping.");

        var source = Path.Combine(_tempDir, "src.big");
        var link   = Path.Combine(_tempDir, "sub", "nested", "link.big");
        File.WriteAllText(source, "nested link content");

        var result = SymLinkService.CreateSymbolicLink(link, source);

        result.Should().BeTrue();
        File.Exists(link).Should().BeTrue();
        File.ReadAllText(link).Should().Be("nested link content");
    }
}
