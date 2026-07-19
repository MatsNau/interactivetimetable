using Timetable.Domain.Planning;

namespace Timetable.Application.Persistence;

/// <summary>
/// Versionsmarke der gespeicherten Plandatei (Zeitstempel des letzten Schreibzugriffs).
/// Wird beim Laden gemerkt und beim Speichern zur Konflikterkennung verglichen.
/// </summary>
public readonly record struct PlanVersionToken(DateTimeOffset LastWriteTimeUtc);

/// <summary>Ein geladener Plan zusammen mit der Version, auf der er basiert.</summary>
public sealed record PlanDocument(ProjectPlan Plan, PlanVersionToken Version);

public interface IPlanRepository
{
    Task<bool> ExistsAsync(CancellationToken ct = default);

    Task<PlanDocument> LoadAsync(CancellationToken ct = default);

    /// <summary>
    /// Speichert den Plan. Weicht die Datei inzwischen von <paramref name="expectedVersion"/> ab,
    /// hat jemand anderes gespeichert und es wird eine <see cref="PlanConflictException"/> geworfen —
    /// außer <paramref name="overwrite"/> ist gesetzt (bewusstes Überschreiben nach Rückfrage).
    /// </summary>
    Task<PlanVersionToken> SaveAsync(
        ProjectPlan plan,
        PlanVersionToken expectedVersion,
        bool overwrite = false,
        CancellationToken ct = default);
}

public sealed class PlanConflictException(PlanVersionToken expected, PlanVersionToken actual)
    : Exception("Die Plandatei wurde zwischenzeitlich von jemand anderem gespeichert.")
{
    public PlanVersionToken Expected { get; } = expected;
    public PlanVersionToken Actual { get; } = actual;
}
