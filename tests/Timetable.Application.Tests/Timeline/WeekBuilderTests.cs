using Timetable.Application.Timeline;
using Timetable.Domain.Calendar;
using Timetable.Domain.People;
using Timetable.Domain.Planning;
using Timetable.Domain.Time;
using Xunit;

namespace Timetable.Application.Tests.Timeline;

public class WeekBuilderTests
{
    // KW 30/2026: Mo 20.07. bis So 26.07.
    private static readonly IsoWeek Week = new(2026, 30);
    private static readonly DateOnly Today = new(2026, 7, 22);

    private static WeekModel BuildModel(ProjectPlan plan) => WeekBuilder.Build(plan, Week, Today);

    private static ProjectPlan PlanWithTask(TaskItem task) => new()
    {
        Title = "Test",
        Projects =
        [
            new Project
            {
                Name = "P",
                Milestones = [new Milestone { Title = "M", Tasks = [task] }],
            },
        ],
    };

    [Fact]
    public void Woche_hat_sieben_Tagesspalten_mit_Wochenende()
    {
        var model = BuildModel(new ProjectPlan { Title = "Test" });

        Assert.Equal(7, model.Days.Count);
        Assert.Equal(new DayColumn(new DateOnly(2026, 7, 20), "Mo 20.07.", IsWeekend: false), model.Days[0]);
        Assert.Equal(new DayColumn(new DateOnly(2026, 7, 25), "Sa 25.07.", IsWeekend: true), model.Days[5]);
        Assert.Equal(new DayColumn(new DateOnly(2026, 7, 26), "So 26.07.", IsWeekend: true), model.Days[6]);
        Assert.Equal(2, model.TodayColumn); // Mittwoch 22.07.
    }

    [Fact]
    public void Aufgabe_innerhalb_der_Woche_belegt_ihre_Tage()
    {
        var plan = PlanWithTask(new TaskItem
        {
            Title = "T",
            Start = new DateOnly(2026, 7, 21),
            End = new DateOnly(2026, 7, 23),
        });

        var row = Assert.IsType<WeekTaskRow>(BuildModel(plan).Rows[2]);

        Assert.Equal(1, row.StartColumn);
        Assert.Equal(3, row.ColumnCount);
        Assert.False(row.ContinuesBefore);
        Assert.False(row.ContinuesAfter);
        Assert.Equal("21.07.–23.07.", row.DateText);
    }

    [Fact]
    public void Ueberlappende_Aufgabe_wird_auf_die_Woche_beschnitten()
    {
        var plan = PlanWithTask(new TaskItem
        {
            Title = "T",
            Start = new DateOnly(2026, 7, 15),
            End = new DateOnly(2026, 7, 29),
        });

        var row = Assert.IsType<WeekTaskRow>(BuildModel(plan).Rows[2]);

        Assert.Equal(0, row.StartColumn);
        Assert.Equal(7, row.ColumnCount);
        Assert.True(row.ContinuesBefore);
        Assert.True(row.ContinuesAfter);
    }

    [Fact]
    public void Aufgabe_ausserhalb_der_Woche_erscheint_nicht()
    {
        var plan = PlanWithTask(new TaskItem
        {
            Title = "T",
            Start = new DateOnly(2026, 7, 27),
            End = new DateOnly(2026, 7, 31),
        });

        Assert.DoesNotContain(BuildModel(plan).Rows, row => row is WeekTaskRow);
    }

    [Fact]
    public void Tagesgenauer_Meilenstein_markiert_seinen_Tag()
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
                        new Milestone { Title = "M", PlannedDate = FuzzyDate.FromDay(new DateOnly(2026, 7, 24)) },
                    ],
                },
            ],
        };

        var row = Assert.IsType<WeekMilestoneRow>(BuildModel(plan).Rows[1]);

        Assert.Equal(4, row.MarkerStartColumn); // Freitag
        Assert.Equal(1, row.MarkerColumnCount);
    }

    [Fact]
    public void Meilenstein_ausserhalb_der_Woche_hat_keinen_Marker()
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
                        new Milestone { Title = "M", PlannedDate = FuzzyDate.FromDay(new DateOnly(2026, 8, 3)) },
                    ],
                },
            ],
        };

        var row = Assert.IsType<WeekMilestoneRow>(BuildModel(plan).Rows[1]);

        Assert.Null(row.MarkerStartColumn);
    }

    [Fact]
    public void Monatsgenauer_Meilenstein_belegt_die_ganze_Woche()
    {
        var plan = new ProjectPlan
        {
            Title = "Test",
            Projects =
            [
                new Project
                {
                    Name = "P",
                    Milestones = [new Milestone { Title = "M", PlannedDate = FuzzyDate.FromMonth(2026, 7) }],
                },
            ],
        };

        var row = Assert.IsType<WeekMilestoneRow>(BuildModel(plan).Rows[1]);

        Assert.Equal(0, row.MarkerStartColumn);
        Assert.Equal(7, row.MarkerColumnCount);
    }

    [Fact]
    public void Ferienband_wird_auf_die_Woche_beschnitten()
    {
        var plan = new ProjectPlan
        {
            Title = "Test",
            Holidays =
            [
                new HolidayPeriod { Name = "Theaterferien", Start = new DateOnly(2026, 7, 6), End = new DateOnly(2026, 8, 16) },
                new HolidayPeriod { Name = "Herbstferien", Start = new DateOnly(2026, 10, 12), End = new DateOnly(2026, 10, 24) },
            ],
        };

        var model = BuildModel(plan);

        var band = Assert.Single(model.Holidays);
        Assert.Equal("Theaterferien", band.Name);
        Assert.Equal(0, band.StartColumn);
        Assert.Equal(7, band.ColumnCount);
    }

    [Fact]
    public void Zugeordnete_Personen_werden_als_Kuerzel_angezeigt()
    {
        var person = new Person { ShortCode = "MF" };
        var plan = PlanWithTask(new TaskItem
        {
            Title = "T",
            Start = new DateOnly(2026, 7, 20),
            End = new DateOnly(2026, 7, 21),
            AssigneeIds = [person.Id, Guid.NewGuid()],
        });
        plan.People = [person];

        var row = Assert.IsType<WeekTaskRow>(BuildModel(plan).Rows[2]);

        Assert.Equal("MF, ?", row.Assignees);
    }
}
