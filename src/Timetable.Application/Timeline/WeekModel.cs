using Timetable.Domain.Planning;
using Timetable.Domain.Time;

namespace Timetable.Application.Timeline;

/// <summary>Eine Tagesspalte der Wochenansicht, z. B. "Mo 20.07.".</summary>
public sealed record DayColumn(DateOnly Date, string Label, bool IsWeekend);

/// <summary>Ein Band über zusammenhängenden Tagesspalten (Ferien oder externes Event).</summary>
public sealed record DayBandSegment(Guid SourceId, string Name, int StartColumn, int ColumnCount, bool IsHighlighted);

public abstract record WeekRow;

/// <summary>Graue Gruppenzeile eines übergeordneten Projekts (Wochenansicht).</summary>
public sealed record WeekProjectRow(Guid ProjectId, string Name) : WeekRow;

/// <summary>
/// Meilensteinzeile der Wochenansicht; Marker nur, wenn der Termin
/// die dargestellte Woche berührt (Spalten ggf. auf die Woche beschnitten).
/// </summary>
public sealed record WeekMilestoneRow(
    Guid MilestoneId,
    string Title,
    string DateText,
    string Lead,
    string Participants,
    MilestoneStatus Status,
    int? MarkerStartColumn,
    int MarkerColumnCount) : WeekRow;

/// <summary>
/// Eine Aufgabenzeile; nur Aufgaben, die die Woche berühren, werden aufgenommen.
/// Continues-Flags zeigen an, dass die Aufgabe vor bzw. nach der Woche weiterläuft.
/// </summary>
public sealed record WeekTaskRow(
    Guid TaskId,
    Guid MilestoneId,
    string Title,
    string DateText,
    string Assignees,
    MilestoneStatus Status,
    int StartColumn,
    int ColumnCount,
    bool ContinuesBefore,
    bool ContinuesAfter) : WeekRow;

/// <summary>
/// Die fertig berechnete Wochenansicht: 7 Tagesspalten, Bänder und Zeilen.
/// Reine Daten ohne UI-Abhängigkeit — die WPF-Schicht muss nur noch zeichnen.
/// </summary>
public sealed record WeekModel(
    IsoWeek Week,
    IReadOnlyList<DayColumn> Days,
    IReadOnlyList<DayBandSegment> Holidays,
    IReadOnlyList<DayBandSegment> Events,
    IReadOnlyList<WeekRow> Rows,
    int? TodayColumn);
