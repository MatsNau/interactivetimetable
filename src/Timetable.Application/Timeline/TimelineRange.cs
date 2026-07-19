using Timetable.Domain.Planning;
using Timetable.Domain.Time;

namespace Timetable.Application.Timeline;

/// <summary>Der dargestellte Ausschnitt der Zeitachse, von KW bis KW (einschließlich).</summary>
public sealed record TimelineRange
{
    public IsoWeek From { get; }
    public IsoWeek To { get; }

    public TimelineRange(IsoWeek from, IsoWeek to)
    {
        if (from > to)
            throw new ArgumentException($"Der Bereich beginnt ({from}) nach seinem Ende ({to}).", nameof(from));

        From = from;
        To = to;
    }

    public int ColumnCount => ColumnOffset(To) + 1;

    /// <summary>Spaltenversatz relativ zum Anfang; kann außerhalb des Bereichs liegen (negativ bzw. ≥ ColumnCount).</summary>
    public int ColumnOffset(IsoWeek week) => (week.Monday.DayNumber - From.Monday.DayNumber) / 7;

    /// <summary>Spaltenindex der Woche oder null, wenn sie außerhalb des Bereichs liegt.</summary>
    public int? ColumnOf(IsoWeek week)
    {
        var offset = ColumnOffset(week);
        return offset >= 0 && offset < ColumnCount ? offset : null;
    }

    public IReadOnlyList<IsoWeek> EnumerateWeeks() => IsoWeek.Range(From, To).ToList();

    /// <summary>
    /// Kleinster Bereich, der alle Meilensteine, Ferien und Events des Plans abdeckt.
    /// Bei leerem Plan: ±8 Wochen um <paramref name="fallbackAnchor"/>.
    /// </summary>
    public static TimelineRange Covering(ProjectPlan plan, DateOnly fallbackAnchor)
    {
        var weeks = new List<IsoWeek>();

        foreach (var milestone in plan.Projects.SelectMany(p => p.Milestones))
        {
            var covered = milestone.PlannedDate.CoveredWeeks();
            weeks.Add(covered[0]);
            weeks.Add(covered[^1]);
        }

        foreach (var holiday in plan.Holidays)
        {
            weeks.Add(IsoWeek.FromDate(holiday.Start));
            weeks.Add(IsoWeek.FromDate(holiday.End));
        }

        foreach (var externalEvent in plan.ExternalEvents)
        {
            weeks.Add(IsoWeek.FromDate(externalEvent.Start));
            weeks.Add(IsoWeek.FromDate(externalEvent.End));
        }

        if (weeks.Count == 0)
        {
            return new TimelineRange(
                IsoWeek.FromDate(fallbackAnchor.AddDays(-8 * 7)),
                IsoWeek.FromDate(fallbackAnchor.AddDays(8 * 7)));
        }

        return new TimelineRange(weeks.Min(), weeks.Max());
    }
}
