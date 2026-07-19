using Timetable.Application.Collaboration;
using Timetable.Application.Persistence;

namespace Timetable.Application.Workspaces;

/// <summary>
/// Bündelt Persistenz und Anwesenheit für eine konkret geöffnete Plandatei.
/// Dispose zieht die eigene Anwesenheit zurück.
/// </summary>
public interface IPlanWorkspace : IAsyncDisposable
{
    string FilePath { get; }

    IPlanRepository Repository { get; }

    IPresenceService Presence { get; }
}

/// <summary>Erzeugt ein Workspace für den zur Laufzeit gewählten Dateipfad.</summary>
public interface IPlanWorkspaceFactory
{
    IPlanWorkspace Create(string filePath);
}
