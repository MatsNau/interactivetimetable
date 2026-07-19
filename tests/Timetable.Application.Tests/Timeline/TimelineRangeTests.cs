using Timetable.Application.Timeline;
using Timetable.Domain.Calendar;
using Timetable.Domain.Planning;
using Timetable.Domain.Time;
using Xunit;

namespace Timetable.Application.Tests.Timeline;

public class TimelineRangeTests
{
    [Fact]
    public void Anfang_nach_Ende_wird_abgelehnt()
    {
        Assert.Throws<ArgumentException>(() =>
            new TimelineRange(new IsoWeek(2027, 2), new IsoWeek(2026, 50)));
    }

    [Fact]
    public void ColumnCount_zaehlt_ueber_den_Jahreswechsel()
    {
        // KW 50, 51, 52, 53/2026, KW 1, 2/2027 → 6 Spalten
        var range = new TimelineRange(new IsoWeek(2026, 50), new IsoWeek(2027, 2));

        Assert.Equal(6, range.ColumnCount);
    }

    [Fact]
    public void ColumnOf_liefert_Index_oder_null()
    {
        var range = new TimelineRange(new IsoWeek(2026, 50), new IsoWeek(2027, 2));

        Assert.Equal(0, range.ColumnOf(new IsoWeek(2026, 50)));
        Assert.Equal(3, range.ColumnOf(new IsoWeek(2026, 53)));
        Assert.Equal(4, range.ColumnOf(new IsoWeek(2027, 1)));
        Assert.Null(range.ColumnOf(new IsoWeek(2026, 49)));
        Assert.Null(range.ColumnOf(new IsoWeek(2027, 3)));
    }

    [Fact]
    public void Covering_umfasst_Meilensteine_Ferien_und_Events()
    {
        var plan = new ProjectPlan
        {
            Title = "Test",
            Projects =
            [
                new Project
                {
                    Name = "P",
                    Milestones =
                    [
                        // monatsgenau: reicht bis in die Woche des 30.06.2027 (KW 26)
                        new Milestone { Title = "M", PlannedDate = FuzzyDate.FromMonth(2027, 6) },
                    ],
                },
            ],
            Holidays =
            [
                new HolidayPeriod { Name = "F", Start = new DateOnly(2026, 10, 12), End = new DateOnly(2026, 10, 24) },
            ],
        };

        var range = TimelineRange.Covering(plan, fallbackAnchor: new DateOnly(2026, 7, 19));

        Assert.Equal(IsoWeek.FromDate(new DateOnly(2026, 10, 12)), range.From);
        Assert.Equal(new IsoWeek(2027, 26), range.To);
    }

    [Fact]
    public void Covering_bei_leerem_Plan_liegt_um_den_Anker()
    {
        var plan = new ProjectPlan { Title = "Leer" };
        var anchor = new DateOnly(2026, 7, 19);

        var range = TimelineRange.Covering(plan, anchor);

        Assert.True(range.From <= IsoWeek.FromDate(anchor));
        Assert.True(range.To >= IsoWeek.FromDate(anchor));
        Assert.Equal(17, range.ColumnCount); // ±8 Wochen + aktuelle Woche
    }
}
