using Timetable.Application.Collaboration;
using Timetable.Application.Persistence;
using Timetable.Application.Workspaces;
using Timetable.Infrastructure.Collaboration;
using Timetable.Infrastructure.Persistence;

namespace Timetable.Infrastructure.Workspaces;

public sealed class PlanWorkspaceFactory : IPlanWorkspaceFactory
{
    public IPlanWorkspace Create(string filePath) => new PlanWorkspace(filePath);

    private sealed class PlanWorkspace : IPlanWorkspace
    {
        public PlanWorkspace(string filePath)
        {
            FilePath = filePath;
            Repository = new JsonPlanRepository(new PlanFileOptions { FilePath = filePath });
            Presence = new FilePresenceService(new PresenceOptions { PlanFilePath = filePath });
        }

        public string FilePath { get; }

        public IPlanRepository Repository { get; }

        public IPresenceService Presence { get; }

        public ValueTask DisposeAsync() => Presence.DisposeAsync();
    }
}
