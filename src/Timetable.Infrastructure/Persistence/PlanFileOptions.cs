namespace Timetable.Infrastructure.Persistence;

/// <summary>Konfiguration der gemeinsamen Plandatei (typischerweise auf einem Netzlaufwerk).</summary>
public sealed class PlanFileOptions
{
    public required string FilePath { get; init; }

    /// <summary>Anzahl rotierender Backups (plan.json.backup-1 … -N); 0 deaktiviert Backups.</summary>
    public int BackupCount { get; init; } = 5;
}
