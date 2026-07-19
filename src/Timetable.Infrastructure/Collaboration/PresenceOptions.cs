namespace Timetable.Infrastructure.Collaboration;

/// <summary>Konfiguration der Anwesenheitsanzeige rund um die gemeinsame Plandatei.</summary>
public sealed class PresenceOptions
{
    public required string PlanFilePath { get; init; }

    public string UserName { get; init; } = Environment.UserName;

    /// <summary>Wie oft der eigene Heartbeat erneuert wird.</summary>
    public TimeSpan HeartbeatInterval { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Ab wann fremde Heartbeats als verwaist gelten (Absturz, Rechner aus).
    /// Sollte deutlich größer als <see cref="HeartbeatInterval"/> sein.
    /// </summary>
    public TimeSpan StaleAfter { get; init; } = TimeSpan.FromSeconds(90);
}
