using Timetable.Domain.Planning;
using Timetable.Domain.Time;

namespace Timetable.Application.Timeline;

/// <summary>Projiziert einen <see cref="ProjectPlan"/> auf die Tagesspalten der Wochenansicht.</summary>
public static class WeekBuilder
{
    private static readonly string[] DayNames = ["Mo", "Di", "Mi", "Do", "Fr", "Sa", "So"];

    public static WeekModel Build(ProjectPlan plan, IsoWeek week, DateOnly today)
    {
        var monday = week.Monday;
        var sunday = week.Sunday;

        var days = Enumerable.Range(0, 7)
            .Select(offset =>
            {
                var date = monday.AddDays(offset);
                return new DayColumn(date, $"{DayNames[offset]} {date:dd.MM.}", offset >= 5);
            })
            .ToList();

        var holidays = plan.Holidays
            .Select(h => MapBand(monday, sunday, h.Id, h.Name, h.Start, h.End, isHighlighted: false))
            .OfType<DayBandSegment>()
            .ToList();

        var events = plan.ExternalEvents
            .Select(e => MapBand(monday, sunday, e.Id, e.Name, e.Start, e.End, e.IsHighlighted))
            .OfType<DayBandSegment>()
            .ToList();

        var rows = new List<WeekRow>();
        foreach (var project in plan.Projects)
        {
            rows.Add(new WeekProjectRow(project.Id, project.Name));

            foreach (var milestone in project.Milestones)
            {
                var marker = MarkerColumns(monday, sunday, milestone.PlannedDate);
                rows.Add(new WeekMilestoneRow(
                    milestone.Id,
                    milestone.Title,
                    milestone.PlannedDate.ToDisplayString(),
                    PersonLabels.ShortCode(plan, milestone.LeadId),
                    PersonLabels.ShortCodes(plan, milestone.ParticipantIds),
                    milestone.Status,
                    marker?.StartColumn,
                    marker?.ColumnCount ?? 0));

                foreach (var task in milestone.Tasks)
                {
                    if (task.End < monday || task.Start > sunday)
                        continue;

                    var startColumn = Math.Max(DayOffset(monday, task.Start), 0);
                    var endColumn = Math.Min(DayOffset(monday, task.End), 6);
                    rows.Add(new WeekTaskRow(
                        task.Id,
                        milestone.Id,
                        task.Title,
                        $"{task.Start:dd.MM.}–{task.End:dd.MM.}",
                        PersonLabels.ShortCodes(plan, task.AssigneeIds),
                        task.Status,
                        startColumn,
                        endColumn - startColumn + 1,
                        ContinuesBefore: task.Start < monday,
                        ContinuesAfter: task.End > sunday));
                }
            }
        }

        var todayOffset = DayOffset(monday, today);
        return new WeekModel(
            week, days, holidays, events, rows,
            todayOffset is >= 0 and <= 6 ? todayOffset : null);
    }

    private static int DayOffset(DateOnly monday, DateOnly date) => date.DayNumber - monday.DayNumber;

    /// <summary>Der von einem Termin belegte Datumsbereich (Tag, ISO-Woche bzw. ganzer Monat).</summary>
    private static (DateOnly Start, DateOnly End) CoveredDates(FuzzyDate date) => date.Precision switch
    {
        FuzzyDatePrecision.Day => (date.Anchor, date.Anchor),
        FuzzyDatePrecision.Week => (date.Anchor, date.Anchor.AddDays(6)),
        _ => (date.Anchor, new DateOnly(date.Anchor.Year, date.Anchor.Month,
            DateTime.DaysInMonth(date.Anchor.Year, date.Anchor.Month))),
    };

    private static (int StartColumn, int ColumnCount)? MarkerColumns(DateOnly monday, DateOnly sunday, FuzzyDate date)
    {
        var (start, end) = CoveredDates(date);
        if (end < monday || start > sunday)
            return null;

        var startColumn = Math.Max(DayOffset(monday, start), 0);
        var endColumn = Math.Min(DayOffset(monday, end), 6);
        return (startColumn, endColumn - startColumn + 1);
    }

    private static DayBandSegment? MapBand(
        DateOnly monday, DateOnly sunday, Guid sourceId, string name, DateOnly start, DateOnly end, bool isHighlighted)
    {
        if (end < monday || start > sunday)
            return null;

        var startColumn = Math.Max(DayOffset(monday, start), 0);
        var endColumn = Math.Min(DayOffset(monday, end), 6);
        return new DayBandSegment(sourceId, name, startColumn, endColumn - startColumn + 1, isHighlighted);
    }
}
