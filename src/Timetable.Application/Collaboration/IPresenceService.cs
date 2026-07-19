namespace Timetable.Application.Collaboration;

/// <summary>Ein anderer Benutzer, der den Plan gerade geöffnet hat.</summary>
public sealed record PresenceInfo(string UserName, DateTimeOffset LastHeartbeatUtc);

/// <summary>
/// Lightweight-Anwesenheitsanzeige über Sidecar-Dateien neben der Plandatei:
/// Beim Start wird die eigene Anwesenheit angekündigt und zyklisch erneuert (Heartbeat),
/// beim Beenden zurückgezogen. Veraltete Heartbeats anderer gelten als "nicht mehr da".
/// </summary>
public interface IPresenceService : IAsyncDisposable
{
    /// <summary>Eigene Anwesenheit ankündigen und Heartbeat starten.</summary>
    Task AnnounceAsync(CancellationToken ct = default);

    /// <summary>Alle anderen aktuell anwesenden Benutzer (ohne den eigenen Eintrag).</summary>
    Task<IReadOnlyList<PresenceInfo>> GetOtherUsersAsync(CancellationToken ct = default);

    /// <summary>Eigene Anwesenheit zurückziehen (regulärer Programmende-Pfad).</summary>
    Task WithdrawAsync(CancellationToken ct = default);
}
