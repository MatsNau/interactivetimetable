using Timetable.Domain.Planning;
using Timetable.Domain.Time;

namespace Timetable.Application.Timeline;

/// <summary>Eine Monatsgruppe über den KW-Spalten, z. B. "Juni-27" über 4 Wochen.</summary>
public sealed record MonthGroup(int Year, int Month, int StartColumn, int ColumnCount, string Label);

/// <summary>Ein Band über zusammenhängenden Spalten (Ferien oder externes Event).</summary>
public sealed record BandSegment(Guid SourceId, string Name, int StartColumn, int ColumnCount, bool IsHighlighted);

public enum MarkerKind
{
    /// <summary>Tagesgenau: Marker mit Tageszahl, wie "22" im Excel.</summary>
    ExactDay,

    /// <summary>Wochengenau: Marker mit "x", wie im Excel.</summary>
    WeekOnly,

    /// <summary>Monatsgenau: Balken über alle Wochen des Monats.</summary>
    MonthSpan,
}

/// <summary>Position und Darstellung eines Meilensteins auf der Zeitachse.</summary>
public sealed record MilestoneMarker(int StartColumn, int ColumnCount, MarkerKind Kind, string Label);

public abstract record TimelineRow;

/// <summary>Graue Gruppenzeile eines übergeordneten Projekts.</summary>
public sealed record ProjectHeaderRow(Guid ProjectId, string Name, string Lead, string Participants) : TimelineRow;

/// <summary>Eine Meilensteinzeile; Marker ist null, wenn der Termin außerhalb des Bereichs liegt.</summary>
public sealed record MilestoneRow(
    Guid MilestoneId,
    string Title,
    string DateText,
    string Lead,
    string Participants,
    string Note,
    MilestoneStatus Status,
    MilestoneMarker? Marker) : TimelineRow;

/// <summary>
/// Die fertig berechnete Jahresansicht: Spaltenstruktur, Bänder und Zeilen.
/// Reine Daten ohne UI-Abhängigkeit — die WPF-Schicht muss nur noch zeichnen.
/// </summary>
public sealed record TimelineModel(
    TimelineRange Range,
    IReadOnlyList<IsoWeek> Weeks,
    IReadOnlyList<MonthGroup> Months,
    IReadOnlyList<BandSegment> Holidays,
    IReadOnlyList<BandSegment> Events,
    IReadOnlyList<TimelineRow> Rows,
    int? TodayColumn);
