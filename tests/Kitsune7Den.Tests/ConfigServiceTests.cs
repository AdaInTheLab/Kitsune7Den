using System.IO;
using Kitsune7Den.Models;
using Kitsune7Den.Services;

namespace Kitsune7Den.Tests;

/// <summary>
/// Tests the serverconfig.xml parser and the world discovery scanner.
/// </summary>
public class ConfigServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly AppSettings _settings;
    private readonly ConfigService _service;

    public ConfigServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "Kitsune7DenTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempRoot);
        _settings = new AppSettings { ServerDirectory = _tempRoot };
        _service = new ConfigService(_settings);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_tempRoot)) Directory.Delete(_tempRoot, recursive: true); }
        catch { /* best effort */ }
    }

    private void WriteServerConfig(string xml) =>
        File.WriteAllText(Path.Combine(_tempRoot, "serverconfig.xml"), xml);

    [Fact]
    public void LoadConfig_ReturnsEmptyList_WhenFileMissing()
    {
        var props = _service.LoadConfig();
        Assert.Empty(props);
    }

    [Fact]
    public void LoadConfig_ParsesPropertiesFromXml()
    {
        WriteServerConfig(@"<?xml version=""1.0""?>
<ServerSettings>
  <property name=""ServerName"" value=""Test Server"" />
  <property name=""ServerMaxPlayerCount"" value=""8"" />
  <property name=""GameWorld"" value=""Navezgane"" />
</ServerSettings>");

        var props = _service.LoadConfig();
        Assert.Equal(3, props.Count);

        var name = props.First(p => p.Name == "ServerName");
        Assert.Equal("Test Server", name.Value);
        Assert.Equal("Core", name.Category);

        var maxPlayers = props.First(p => p.Name == "ServerMaxPlayerCount");
        Assert.Equal("8", maxPlayers.Value);
        Assert.Equal("Network", maxPlayers.Category);
    }

    [Fact]
    public void LoadConfig_EnrichesWithFieldDefinitions()
    {
        WriteServerConfig(@"<?xml version=""1.0""?>
<ServerSettings>
  <property name=""GameDifficulty"" value=""2"" />
</ServerSettings>");

        var props = _service.LoadConfig();
        var diff = Assert.Single(props);
        Assert.Equal("Gameplay", diff.Category);
        Assert.Equal(ConfigFieldType.Select, diff.FieldType);
        Assert.NotNull(diff.Options);
        Assert.Contains("2", diff.Options);
    }

    [Fact]
    public void LoadConfig_UnknownPropertyFallsIntoOtherCategory()
    {
        WriteServerConfig(@"<?xml version=""1.0""?>
<ServerSettings>
  <property name=""SomeCustomModProperty"" value=""xyz"" />
</ServerSettings>");

        var props = _service.LoadConfig();
        var prop = Assert.Single(props);
        Assert.Equal("Other", prop.Category);
        Assert.Equal(ConfigFieldType.Text, prop.FieldType);
    }

    [Fact]
    public void SaveConfig_CreatesBackupFile()
    {
        WriteServerConfig(@"<?xml version=""1.0""?>
<ServerSettings>
  <property name=""ServerName"" value=""Original"" />
</ServerSettings>");

        var props = _service.LoadConfig();
        props[0].Value = "Updated";
        var ok = _service.SaveConfig(props);

        Assert.True(ok);
        Assert.True(File.Exists(Path.Combine(_tempRoot, "serverconfig.xml.bak")));

        // Reload and verify the new value is written
        var reloaded = _service.LoadConfig();
        Assert.Equal("Updated", reloaded[0].Value);

        // And the backup still has the original
        var bak = File.ReadAllText(Path.Combine(_tempRoot, "serverconfig.xml.bak"));
        Assert.Contains("Original", bak);
    }

    [Fact]
    public void DiscoverWorlds_AlwaysIncludesRWG()
    {
        var worlds = _service.DiscoverWorlds();
        Assert.Contains("RWG", worlds);
    }

    [Fact]
    public void DiscoverWorlds_FindsBuiltInWorldsWithMainTtw()
    {
        var worldsDir = Path.Combine(_tempRoot, "Data", "Worlds");
        Directory.CreateDirectory(Path.Combine(worldsDir, "Navezgane"));
        Directory.CreateDirectory(Path.Combine(worldsDir, "PregenTest"));
        Directory.CreateDirectory(Path.Combine(worldsDir, "NotAWorld")); // no main.ttw

        File.WriteAllText(Path.Combine(worldsDir, "Navezgane", "main.ttw"), "");
        File.WriteAllText(Path.Combine(worldsDir, "PregenTest", "main.ttw"), "");

        var worlds = _service.DiscoverWorlds();
        Assert.Contains("Navezgane", worlds);
        Assert.Contains("PregenTest", worlds);
        Assert.DoesNotContain("NotAWorld", worlds);
    }

    [Fact]
    public void GetRawConfig_ReturnsFileContents()
    {
        const string xml = @"<?xml version=""1.0""?>
<ServerSettings>
  <property name=""ServerName"" value=""Test"" />
</ServerSettings>";
        WriteServerConfig(xml);

        var raw = _service.GetRawConfig();
        Assert.NotNull(raw);
        Assert.Contains("ServerName", raw);
    }

    [Fact]
    public void SaveRawConfig_RejectsInvalidXml()
    {
        WriteServerConfig("<?xml version=\"1.0\"?><ServerSettings></ServerSettings>");
        var ok = _service.SaveRawConfig("this is not xml at all <unclosed");
        Assert.False(ok);
    }
}
