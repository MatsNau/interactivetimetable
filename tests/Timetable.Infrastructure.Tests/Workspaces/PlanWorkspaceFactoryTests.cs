using System.IO;
using Timetable.Domain.Planning;
using Timetable.Infrastructure.Workspaces;
using Xunit;

namespace Timetable.Infrastructure.Tests.Workspaces;

public sealed class PlanWorkspaceFactoryTests : IDisposable
{
    private readonly string _directory;
    private readonly string _planPath;

    public PlanWorkspaceFactoryTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "timetable-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_directory);
        _planPath = Path.Combine(_directory, "plan.json");
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public async Task Workspace_buendelt_Persistenz_und_Anwesenheit_fuer_die_Datei()
    {
        var factory = new PlanWorkspaceFactory();
        await using var workspace = factory.Create(_planPath);

        Assert.Equal(_planPath, workspace.FilePath);

        await workspace.Repository.SaveAsync(new ProjectPlan { Title = "Test" }, expectedVersion: default);
        var (loaded, _) = await workspace.Repository.LoadAsync();
        Assert.Equal("Test", loaded.Title);

        await workspace.Presence.AnnounceAsync();
        Assert.Single(Directory.GetFiles(_directory, "plan.json.presence-*"));
    }

    [Fact]
    public async Task DisposeAsync_zieht_die_Anwesenheit_zurueck()
    {
        var workspace = new PlanWorkspaceFactory().Create(_planPath);
        await workspace.Presence.AnnounceAsync();

        await workspace.DisposeAsync();

        Assert.Empty(Directory.GetFiles(_directory, "plan.json.presence-*"));
    }
}
