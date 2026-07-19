using Timetable.Domain.Planning;
using Timetable.Domain.Time;

namespace Timetable.Application.Timeline;

/// <summary>Projiziert einen <see cref="ProjectPlan"/> auf das KW-Raster der Jahresansicht.</summary>
public static class TimelineBuilder
{
    // Bewusst eigene Namen statt CultureInfo, damit die Beschriftung nicht von ICU-Daten abhängt.
    private static readonly string[] MonthNames =
        ["Jan", "Feb", "März", "Apr", "Mai", "Juni", "Juli", "Aug", "Sep", "Okt", "Nov", "Dez"];

    public static TimelineModel Build(ProjectPlan plan, TimelineRange range, DateOnly today)
    {
        var weeks = range.EnumerateWeeks();

        var holidays = plan.Holidays
            .Select(h => MapBand(range, h.Id, h.Name, h.Start, h.End, isHighlighted: false))
            .OfType<BandSegment>()
            .ToList();

        var events = plan.ExternalEvents
            .Select(e => MapBand(range, e.Id, e.Name, e.Start, e.End, e.IsHighlighted))
            .OfType<BandSegment>()
            .ToList();

        var rows = new List<TimelineRow>();
        foreach (var project in plan.Projects)
        {
            rows.Add(new ProjectHeaderRow(
                project.Id,
                project.Name,
                PersonLabels.ShortCode(plan, project.LeadId),
                PersonLabels.ShortCodes(plan, project.ParticipantIds)));

            foreach (var milestone in project.Milestones)
            {
                rows.Add(new MilestoneRow(
                    milestone.Id,
                    milestone.Title,
                    milestone.PlannedDate.ToDisplayString(),
                    PersonLabels.ShortCode(plan, milestone.LeadId),
                    PersonLabels.ShortCodes(plan, milestone.ParticipantIds),
                    milestone.Note,
                    milestone.Status,
                    BuildMarker(range, milestone.PlannedDate)));
            }
        }

        return new TimelineModel(
            range,
            weeks,
            BuildMonthGroups(weeks),
            holidays,
            events,
            rows,
            range.ColumnOf(IsoWeek.FromDate(today)));
    }

    /// <summary>Eine Woche zählt zu dem Monat, in dem ihr Donnerstag liegt (analog zur ISO-Jahreslogik).</summary>
    private static List<MonthGroup> BuildMonthGroups(IReadOnlyList<IsoWeek> weeks)
    {
        var groups = new List<MonthGroup>();
        var startColumn = 0;
        (int Year, int Month)? current = null;

        for (var column = 0; column < weeks.Count; column++)
        {
            var thursday = weeks[column].Monday.AddDays(3);
            var key = (thursday.Year, thursday.Month);

            if (current is null)
            {
                current = key;
                startColumn = column;
            }
            else if (current != key)
            {
                groups.Add(CreateMonthGroup(current.Value, startColumn, column - startColumn));
                current = key;
                startColumn = column;
            }
        }

        if (current is not null)
            groups.Add(CreateMonthGroup(current.Value, startColumn, weeks.Count - startColumn));

        return groups;
    }

    private static MonthGroup CreateMonthGroup((int Year, int Month) key, int startColumn, int columnCount) =>
        new(key.Year, key.Month, startColumn, columnCount,
            $"{MonthNames[key.Month - 1]}-{key.Year % 100:00}");

    private static BandSegment? MapBand(
        TimelineRange range, Guid sourceId, string name, DateOnly start, DateOnly end, bool isHighlighted)
    {
        var firstWeek = IsoWeek.FromDate(start);
        var lastWeek = IsoWeek.FromDate(end);
        if (lastWeek < range.From || firstWeek > range.To)
            return null;

        var startColumn = Math.Max(range.ColumnOffset(firstWeek), 0);
        var endColumn = Math.Min(range.ColumnOffset(lastWeek), range.ColumnCount - 1);
        return new BandSegment(sourceId, name, startColumn, endColumn - startColumn + 1, isHighlighted);
    }

    private static MilestoneMarker? BuildMarker(TimelineRange range, FuzzyDate plannedDate)
    {
        var covered = plannedDate.CoveredWeeks();
        if (covered[^1] < range.From || covered[0] > range.To)
            return null;

        var startColumn = Math.Max(range.ColumnOffset(covered[0]), 0);
        var endColumn = Math.Min(range.ColumnOffset(covered[^1]), range.ColumnCount - 1);

        var (kind, label) = plannedDate.Precision switch
        {
            FuzzyDatePrecision.Day => (MarkerKind.ExactDay, plannedDate.Anchor.Day.ToString()),
            FuzzyDatePrecision.Week => (MarkerKind.WeekOnly, "x"),
            _ => (MarkerKind.MonthSpan, string.Empty),
        };

        return new MilestoneMarker(startColumn, endColumn - startColumn + 1, kind, label);
    }
}
