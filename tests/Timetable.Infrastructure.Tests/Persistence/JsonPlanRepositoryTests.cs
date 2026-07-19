using System.IO;
using Timetable.Application.Persistence;
using Timetable.Domain.Calendar;
using Timetable.Domain.People;
using Timetable.Domain.Planning;
using Timetable.Domain.Time;
using Timetable.Infrastructure.Persistence;
using Xunit;

namespace Timetable.Infrastructure.Tests.Persistence;

public sealed class JsonPlanRepositoryTests : IDisposable
{
    private readonly string _directory;
    private readonly string _planPath;

    public JsonPlanRepositoryTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "timetable-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_directory);
        _planPath = Path.Combine(_directory, "plan.json");
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private JsonPlanRepository CreateRepository(int backupCount = 5) =>
        new(new PlanFileOptions { FilePath = _planPath, BackupCount = backupCount });

    private static ProjectPlan CreateSamplePlan(string title = "NSH Organisationsuntersuchung")
    {
        var lead = new Person { ShortCode = "MWK", DisplayName = "Ministerium" };
        var participant = new Person { ShortCode = "DB" };

        return new ProjectPlan
        {
            Title = title,
            AsOfDate = new DateOnly(2026, 6, 16),
            People = [lead, participant],
            Projects =
            [
                new Project
                {
                    Name = "Übergeordnetes Projekt A",
                    LeadId = lead.Id,
                    ParticipantIds = [participant.Id],
                    Milestones =
                    [
                        new Milestone
                        {
                            Title = "Kickoff",
                            PlannedDate = FuzzyDate.FromDay(new DateOnly(2027, 6, 22)),
                            Status = MilestoneStatus.Done,
                            LeadId = lead.Id,
                            Note = "Auftakt",
                            Tasks =
                            [
                                new TaskItem
                                {
                                    Title = "Agenda vorbereiten",
                                    Start = new DateOnly(2027, 6, 14),
                                    End = new DateOnly(2027, 6, 18),
                                    AssigneeIds = [participant.Id],
                                },
                            ],
                        },
                        new Milestone
                        {
                            Title = "Zwischenbericht",
                            PlannedDate = FuzzyDate.FromMonth(2027, 3),
                        },
                    ],
                },
            ],
            Holidays =
            [
                new HolidayPeriod
                {
                    Name = "Herbstferien",
                    Start = new DateOnly(2026, 10, 12),
                    End = new DateOnly(2026, 10, 24),
                },
            ],
            ExternalEvents =
            [
                new ExternalEvent
                {
                    Name = "PK 27/28",
                    Start = new DateOnly(2027, 4, 27),
                    End = new DateOnly(2027, 4, 28),
                    IsHighlighted = true,
                },
            ],
        };
    }

    [Fact]
    public async Task Erster_SaveAsync_erstellt_die_Datei()
    {
        var repository = CreateRepository();

        Assert.False(await repository.ExistsAsync());
        await repository.SaveAsync(CreateSamplePlan(), expectedVersion: default);
        Assert.True(await repository.ExistsAsync());
    }

    [Fact]
    public async Task Roundtrip_erhaelt_alle_Daten()
    {
        var repository = CreateRepository();
        var original = CreateSamplePlan();

        await repository.SaveAsync(original, expectedVersion: default);
        var (loaded, _) = await repository.LoadAsync();

        Assert.Equal(original.Title, loaded.Title);
        Assert.Equal(original.AsOfDate, loaded.AsOfDate);

        var originalMilestone = original.Projects[0].Milestones[0];
        var loadedMilestone = loaded.Projects[0].Milestones[0];
        Assert.Equal(originalMilestone.Id, loadedMilestone.Id);
        Assert.Equal(originalMilestone.PlannedDate, loadedMilestone.PlannedDate);
        Assert.Equal(MilestoneStatus.Done, loadedMilestone.Status);
        Assert.Equal(original.People[0].Id, loadedMilestone.LeadId);
        Assert.Equal(originalMilestone.Tasks[0].AssigneeIds, loadedMilestone.Tasks[0].AssigneeIds);

        Assert.Equal(FuzzyDate.FromMonth(2027, 3), loaded.Projects[0].Milestones[1].PlannedDate);
        Assert.Equal("Herbstferien", loaded.Holidays[0].Name);
        Assert.True(loaded.ExternalEvents[0].IsHighlighted);
    }

    [Fact]
    public async Task SaveAsync_mit_veralteter_Version_wirft_Konflikt()
    {
        var repository = CreateRepository();
        var version = await repository.SaveAsync(CreateSamplePlan(), expectedVersion: default);

        // Jemand anderes speichert zwischenzeitlich (simuliert über den Zeitstempel).
        File.SetLastWriteTimeUtc(_planPath, DateTime.UtcNow.AddMinutes(1));

        var exception = await Assert.ThrowsAsync<PlanConflictException>(() =>
            repository.SaveAsync(CreateSamplePlan("Meine Änderung"), version));
        Assert.Equal(version, exception.Expected);
        Assert.NotEqual(exception.Expected, exception.Actual);
    }

    [Fact]
    public async Task SaveAsync_mit_overwrite_ueberschreibt_trotz_Konflikt()
    {
        var repository = CreateRepository();
        var version = await repository.SaveAsync(CreateSamplePlan(), expectedVersion: default);
        File.SetLastWriteTimeUtc(_planPath, DateTime.UtcNow.AddMinutes(1));

        await repository.SaveAsync(CreateSamplePlan("Bewusst überschrieben"), version, overwrite: true);

        var (loaded, _) = await repository.LoadAsync();
        Assert.Equal("Bewusst überschrieben", loaded.Title);
    }

    [Fact]
    public async Task SaveAsync_mit_aktueller_Version_speichert_ohne_Konflikt()
    {
        var repository = CreateRepository();
        var version = await repository.SaveAsync(CreateSamplePlan(), expectedVersion: default);

        var newVersion = await repository.SaveAsync(CreateSamplePlan("Version 2"), version);

        var (loaded, loadedVersion) = await repository.LoadAsync();
        Assert.Equal("Version 2", loaded.Title);
        Assert.Equal(newVersion, loadedVersion);
    }

    [Fact]
    public async Task Backups_rotieren_und_aeltestes_faellt_weg()
    {
        var repository = CreateRepository(backupCount: 2);
        var version = await repository.SaveAsync(CreateSamplePlan("v1"), expectedVersion: default);
        version = await repository.SaveAsync(CreateSamplePlan("v2"), version);
        version = await repository.SaveAsync(CreateSamplePlan("v3"), version);
        await repository.SaveAsync(CreateSamplePlan("v4"), version);

        // Aktuell: v4. Backup-1 = v3, Backup-2 = v2, v1 ist herausrotiert.
        Assert.Contains("v3", File.ReadAllText(_planPath + ".backup-1"));
        Assert.Contains("v2", File.ReadAllText(_planPath + ".backup-2"));
        Assert.False(File.Exists(_planPath + ".backup-3"));
    }

    [Fact]
    public async Task Ohne_BackupCount_entstehen_keine_Backups()
    {
        var repository = CreateRepository(backupCount: 0);
        var version = await repository.SaveAsync(CreateSamplePlan("v1"), expectedVersion: default);
        await repository.SaveAsync(CreateSamplePlan("v2"), version);

        Assert.False(File.Exists(_planPath + ".backup-1"));
    }

    [Fact]
    public async Task SaveAsync_laesst_keine_Temp_Datei_zurueck()
    {
        var repository = CreateRepository();
        await repository.SaveAsync(CreateSamplePlan(), expectedVersion: default);

        Assert.False(File.Exists(_planPath + ".tmp"));
    }
}
