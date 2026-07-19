using System.IO;
using Timetable.Infrastructure.Collaboration;
using Xunit;

namespace Timetable.Infrastructure.Tests.Collaboration;

public sealed class FilePresenceServiceTests : IAsyncLifetime
{
    private readonly string _directory;
    private readonly string _planPath;

    public FilePresenceServiceTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "timetable-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_directory);
        _planPath = Path.Combine(_directory, "plan.json");
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        Directory.Delete(_directory, recursive: true);
        return Task.CompletedTask;
    }

    private FilePresenceService CreateService(string userName, TimeSpan? heartbeatInterval = null) =>
        new(new PresenceOptions
        {
            PlanFilePath = _planPath,
            UserName = userName,
            HeartbeatInterval = heartbeatInterval ?? TimeSpan.FromSeconds(30),
            StaleAfter = TimeSpan.FromSeconds(90),
        });

    [Fact]
    public async Task Announce_erstellt_die_Presence_Datei()
    {
        await using var service = CreateService("mats");

        await service.AnnounceAsync();

        Assert.True(File.Exists(_planPath + ".presence-mats"));
    }

    [Fact]
    public async Task Andere_Benutzer_werden_gesehen_man_selbst_nicht()
    {
        await using var mats = CreateService("mats");
        await using var petra = CreateService("petra");
        await mats.AnnounceAsync();
        await petra.AnnounceAsync();

        var seenByMats = await mats.GetOtherUsersAsync();

        var other = Assert.Single(seenByMats);
        Assert.Equal("petra", other.UserName);
    }

    [Fact]
    public async Task Veraltete_Heartbeats_werden_ignoriert()
    {
        await using var mats = CreateService("mats");
        await using var petra = CreateService("petra");
        await mats.AnnounceAsync();
        await petra.AnnounceAsync();

        // Petras Rechner ist "abgestürzt": Heartbeat liegt weit in der Vergangenheit.
        File.SetLastWriteTimeUtc(_planPath + ".presence-petra", DateTime.UtcNow.AddMinutes(-10));

        Assert.Empty(await mats.GetOtherUsersAsync());
    }

    [Fact]
    public async Task Withdraw_entfernt_die_eigene_Datei()
    {
        await using var mats = CreateService("mats");
        await using var petra = CreateService("petra");
        await mats.AnnounceAsync();
        await petra.AnnounceAsync();

        await petra.WithdrawAsync();

        Assert.False(File.Exists(_planPath + ".presence-petra"));
        Assert.Empty(await mats.GetOtherUsersAsync());
    }

    [Fact]
    public async Task Heartbeat_erneuert_den_Zeitstempel_zyklisch()
    {
        await using var service = CreateService("mats", heartbeatInterval: TimeSpan.FromMilliseconds(50));
        await service.AnnounceAsync();

        var presenceFile = _planPath + ".presence-mats";
        var initial = File.GetLastWriteTimeUtc(presenceFile);

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (File.GetLastWriteTimeUtc(presenceFile) <= initial && DateTime.UtcNow < deadline)
            await Task.Delay(25);

        Assert.True(File.GetLastWriteTimeUtc(presenceFile) > initial,
            "Der Heartbeat hat den Zeitstempel nicht erneuert.");
    }

    [Fact]
    public async Task Benutzernamen_mit_Sonderzeichen_funktionieren()
    {
        await using var domainUser = CreateService(@"FIRMA\mats");
        await using var petra = CreateService("petra");
        await domainUser.AnnounceAsync();
        await petra.AnnounceAsync();

        var seenByPetra = await petra.GetOtherUsersAsync();

        // Der Dateiname ist sanitisiert, der Anzeigename bleibt das Original.
        var other = Assert.Single(seenByPetra);
        Assert.Equal(@"FIRMA\mats", other.UserName);
        Assert.True(File.Exists(_planPath + ".presence-FIRMA_mats"));
    }

    [Fact]
    public async Task DisposeAsync_raeumt_die_eigene_Datei_auf()
    {
        var service = CreateService("mats");
        await service.AnnounceAsync();

        await service.DisposeAsync();

        Assert.False(File.Exists(_planPath + ".presence-mats"));
    }

    [Fact]
    public async Task Mehrere_andere_Benutzer_werden_alphabetisch_geliefert()
    {
        await using var mats = CreateService("mats");
        await using var petra = CreateService("petra");
        await using var alex = CreateService("alex");
        await mats.AnnounceAsync();
        await petra.AnnounceAsync();
        await alex.AnnounceAsync();

        var seenByMats = await mats.GetOtherUsersAsync();

        Assert.Equal(["alex", "petra"], seenByMats.Select(p => p.UserName));
    }
}
