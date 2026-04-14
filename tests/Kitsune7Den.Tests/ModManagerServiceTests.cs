using System.IO;
using Kitsune7Den.Models;
using Kitsune7Den.Services;

namespace Kitsune7Den.Tests;

/// <summary>
/// Tests the mod scanner, ModInfo.xml parser, and toggle/delete logic
/// against a temp Mods folder that we build up and tear down per test.
/// </summary>
public class ModManagerServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly AppSettings _settings;
    private readonly ModManagerService _service;

    public ModManagerServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "Kitsune7DenTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(Path.Combine(_tempRoot, "Mods"));
        _settings = new AppSettings { ServerDirectory = _tempRoot };
        _service = new ModManagerService(_settings);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_tempRoot)) Directory.Delete(_tempRoot, recursive: true); }
        catch { /* best effort */ }
    }

    private string CreateMod(string folderName, string? displayName = null, string? version = null, string? author = null)
    {
        var dir = Path.Combine(_tempRoot, "Mods", folderName);
        Directory.CreateDirectory(dir);

        if (displayName is not null || version is not null || author is not null)
        {
            var xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<xml>\n";
            if (displayName is not null) xml += $"  <DisplayName value=\"{displayName}\" />\n";
            xml += $"  <Name value=\"{folderName}\" />\n";
            if (version is not null) xml += $"  <Version value=\"{version}\" />\n";
            if (author is not null) xml += $"  <Author value=\"{author}\" />\n";
            xml += "</xml>";
            File.WriteAllText(Path.Combine(dir, "ModInfo.xml"), xml);
        }

        return dir;
    }

    [Fact]
    public void GetInstalledMods_ReturnsEmpty_WhenNoModsFolder()
    {
        Directory.Delete(Path.Combine(_tempRoot, "Mods"), recursive: true);
        var mods = _service.GetInstalledMods();
        Assert.Empty(mods);
    }

    [Fact]
    public void GetInstalledMods_ParsesModInfoXmlAttributeFormat()
    {
        CreateMod("KitsunePaints", "Kitsune Paints", "1.1.0", "AdaInTheLab");
        var mods = _service.GetInstalledMods();
        var mod = Assert.Single(mods);
        Assert.Equal("Kitsune Paints", mod.DisplayName);
        Assert.Equal("1.1.0", mod.Version);
        Assert.Equal("AdaInTheLab", mod.Author);
        Assert.True(mod.IsEnabled);
    }

    [Fact]
    public void GetInstalledMods_FallsBackToFolderName_WhenNoModInfo()
    {
        CreateMod("SomeBareMod");
        var mods = _service.GetInstalledMods();
        var mod = Assert.Single(mods);
        Assert.Equal("SomeBareMod", mod.DisplayName);
        Assert.Equal("", mod.Version);
    }

    [Fact]
    public void GetInstalledMods_DetectsDisabledSuffix()
    {
        CreateMod("DisabledMod.disabled", "Disabled Mod");
        var mods = _service.GetInstalledMods();
        var mod = Assert.Single(mods);
        Assert.False(mod.IsEnabled);
    }

    [Fact]
    public void GetInstalledMods_ReturnsAllModsSortedByDisplayName()
    {
        CreateMod("Zebra", "Zebra Mod");
        CreateMod("Alpha", "Alpha Mod");
        CreateMod("Middle", "Middle Mod");

        var mods = _service.GetInstalledMods();
        Assert.Equal(3, mods.Count);
        Assert.Equal("Alpha Mod", mods[0].DisplayName);
        Assert.Equal("Middle Mod", mods[1].DisplayName);
        Assert.Equal("Zebra Mod", mods[2].DisplayName);
    }

    [Fact]
    public void ToggleMod_EnableToDisable_RenamesFolderWithDisabledSuffix()
    {
        CreateMod("TestMod", "Test Mod");
        var ok = _service.ToggleMod("TestMod");
        Assert.True(ok);
        Assert.False(Directory.Exists(Path.Combine(_tempRoot, "Mods", "TestMod")));
        Assert.True(Directory.Exists(Path.Combine(_tempRoot, "Mods", "TestMod.disabled")));
    }

    [Fact]
    public void ToggleMod_DisableToEnable_StripsDisabledSuffix()
    {
        CreateMod("TestMod.disabled", "Test Mod");
        var ok = _service.ToggleMod("TestMod.disabled");
        Assert.True(ok);
        Assert.True(Directory.Exists(Path.Combine(_tempRoot, "Mods", "TestMod")));
        Assert.False(Directory.Exists(Path.Combine(_tempRoot, "Mods", "TestMod.disabled")));
    }

    [Fact]
    public void DeleteMod_RemovesFolderRecursively()
    {
        CreateMod("TestMod", "Test Mod");
        var ok = _service.DeleteMod("TestMod");
        Assert.True(ok);
        Assert.False(Directory.Exists(Path.Combine(_tempRoot, "Mods", "TestMod")));
    }

    [Fact]
    public void DeleteMod_RefusesPathTraversalAttempt()
    {
        // Even if someone constructs a path with .. we should stay within Mods
        var ok = _service.DeleteMod("..\\EvilEscape");
        Assert.False(ok);
    }

    [Fact]
    public void InstallFromZip_ExtractsRootFolderStructure()
    {
        // Build a zip containing MyMod/ModInfo.xml
        var tempSource = Path.Combine(_tempRoot, "source");
        var modDir = Path.Combine(tempSource, "MyMod");
        Directory.CreateDirectory(modDir);
        File.WriteAllText(Path.Combine(modDir, "ModInfo.xml"),
            "<?xml version=\"1.0\"?>\n<xml>\n  <DisplayName value=\"My Mod\" />\n</xml>");

        var zipPath = Path.Combine(_tempRoot, "mymod.zip");
        System.IO.Compression.ZipFile.CreateFromDirectory(tempSource, zipPath);

        var installedName = _service.InstallFromZip(zipPath);

        Assert.Equal("MyMod", installedName);
        Assert.True(Directory.Exists(Path.Combine(_tempRoot, "Mods", "MyMod")));
        Assert.True(File.Exists(Path.Combine(_tempRoot, "Mods", "MyMod", "ModInfo.xml")));
    }
}
