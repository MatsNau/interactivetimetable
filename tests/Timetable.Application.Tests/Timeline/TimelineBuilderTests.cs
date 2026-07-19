using Timetable.Application.Timeline;
using Timetable.Domain.Calendar;
using Timetable.Domain.People;
using Timetable.Domain.Planning;
using Timetable.Domain.Time;
using Xunit;

namespace Timetable.Application.Tests.Timeline;

public class TimelineBuilderTests
{
    private static readonly DateOnly Today = new(2026, 12, 16);

    private static TimelineModel BuildModel(ProjectPlan plan, TimelineRange? range = null) =>
        TimelineBuilder.Build(plan, range ?? new TimelineRange(new IsoWeek(2026, 50), new IsoWeek(2027, 5)), Today);

    private static ProjectPlan EmptyPlan() => new() { Title = "Test" };

    private static ProjectPlan PlanWithMilestone(Milestone milestone) => new()
    {
        Title = "Test",
        Projects = [new Project { Name = "P", Milestones = [milestone] }],
    };

    [Fact]
    public void Monatsgruppen_folgen_dem_Donnerstag_der_Woche()
    {
        var model = BuildModel(EmptyPlan());

        // KW 50–53/2026 haben ihre Donnerstage im Dezember, KW 1–4/2027 im Januar, KW 5 im Februar.
        Assert.Equal(3, model.Months.Count);
        Assert.Equal(new MonthGroup(2026, 12, 0, 4, "Dez-26"), model.Months[0]);
        Assert.Equal(new MonthGroup(2027, 1, 4, 4, "Jan-27"), model.Months[1]);
        Assert.Equal(new MonthGroup(2027, 2, 8, 1, "Feb-27"), model.Months[2]);
    }

    [Fact]
    public void Tagesgenauer_Meilenstein_bekommt_Marker_mit_Tageszahl()
    {
        var plan = PlanWithMilestone(new Milestone
        {
            Title = "M",
            PlannedDate = FuzzyDate.FromDay(new DateOnly(2026, 12, 18)), // Freitag der KW 51
        });

        var row = Assert.IsType<MilestoneRow>(BuildModel(plan).Rows[1]);

        Assert.Equal("18.12.2026", row.DateText);
        Assert.Equal(new MilestoneMarker(1, 1, MarkerKind.ExactDay, "18"), row.Marker);
    }

    [Fact]
    public void Wochengenauer_Meilenstein_bekommt_x_Marker()
    {
        var plan = PlanWithMilestone(new Milestone
        {
            Title = "M",
            PlannedDate = FuzzyDate.FromWeek(2027, 2),
        });

        var row = Assert.IsType<MilestoneRow>(BuildModel(plan).Rows[1]);

        Assert.Equal(new MilestoneMarker(5, 1, MarkerKind.WeekOnly, "x"), row.Marker);
    }

    [Fact]
    public void Monatsgenauer_Meilenstein_spannt_alle_Monatswochen()
    {
        var plan = PlanWithMilestone(new Milestone
        {
            Title = "M",
            PlannedDate = FuzzyDate.FromMonth(2027, 1), // KW 53/2026 bis KW 4/2027
        });

        var row = Assert.IsType<MilestoneRow>(BuildModel(plan).Rows[1]);

        Assert.Equal(new MilestoneMarker(3, 5, MarkerKind.MonthSpan, ""), row.Marker);
    }

    [Fact]
    public void Meilenstein_ausserhalb_des_Bereichs_hat_keinen_Marker()
    {
        var plan = PlanWithMilestone(new Milestone
        {
            Title = "M",
            PlannedDate = FuzzyDate.FromDay(new DateOnly(2027, 6, 22)),
        });

        var row = Assert.IsType<MilestoneRow>(BuildModel(plan).Rows[1]);

        Assert.Null(row.Marker);
        Assert.Equal("22.06.2027", row.DateText); // in der Tabelle trotzdem sichtbar
    }

    [Fact]
    public void Ferienband_wird_auf_den_Bereich_zugeschnitten()
    {
        var plan = EmptyPlan();
        plan.Holidays =
        [
            // beginnt vor dem Bereich (KW 49) und endet in KW 51
            new HolidayPeriod { Name = "Ferien", Start = new DateOnly(2026, 12, 2), End = new DateOnly(2026, 12, 19) },
            // liegt komplett außerhalb
            new HolidayPeriod { Name = "Sommer", Start = new DateOnly(2026, 7, 6), End = new DateOnly(2026, 8, 16) },
        ];

        var model = BuildModel(plan);

        var band = Assert.Single(model.Holidays);
        Assert.Equal("Ferien", band.Name);
        Assert.Equal(0, band.StartColumn);
        Assert.Equal(2, band.ColumnCount);
    }

    [Fact]
    public void Events_behalten_ihre_Hervorhebung()
    {
        var plan = EmptyPlan();
        plan.ExternalEvents =
        [
            new ExternalEvent
            {
                Name = "PK 27/28",
                Start = new DateOnly(2027, 1, 12),
                End = new DateOnly(2027, 1, 13),
                IsHighlighted = true,
            },
        ];

        var model = BuildModel(plan);

        var band = Assert.Single(model.Events);
        Assert.True(band.IsHighlighted);
        Assert.Equal(5, band.StartColumn);
        Assert.Equal(1, band.ColumnCount);
    }

    [Fact]
    public void Zeilen_folgen_der_Planreihenfolge_mit_Projektkopf()
    {
        var lead = new Person { ShortCode = "MWK" };
        var participant = new Person { ShortCode = "DB" };
        var plan = new ProjectPlan
        {
            Title = "Test",
            People = [lead, participant],
            Projects =
            [
                new Project
                {
                    Name = "Projekt A",
                    LeadId = lead.Id,
                    ParticipantIds = [participant.Id],
                    Milestones =
                    [
                        new Milestone { Title = "M1", PlannedDate = FuzzyDate.FromDay(new DateOnly(2026, 12, 18)), LeadId = lead.Id },
                        new Milestone { Title = "M2", PlannedDate = FuzzyDate.FromWeek(2027, 2), ParticipantIds = [participant.Id, Guid.NewGuid()] },
                    ],
                },
            ],
        };

        var model = BuildModel(plan);

        Assert.Equal(3, model.Rows.Count);
        var header = Assert.IsType<ProjectHeaderRow>(model.Rows[0]);
        Assert.Equal(("Projekt A", "MWK", "DB"), (header.Name, header.Lead, header.Participants));

        var m2 = Assert.IsType<MilestoneRow>(model.Rows[2]);
        Assert.Equal("DB, ?", m2.Participants); // unbekannte Person wird als "?" markiert
    }

    [Fact]
    public void TodayColumn_zeigt_auf_die_aktuelle_Woche()
    {
        var model = BuildModel(EmptyPlan());

        // 16.12.2026 liegt in KW 51 → Spalte 1
        Assert.Equal(1, model.TodayColumn);
    }

    [Fact]
    public void TodayColumn_ist_null_wenn_heute_ausserhalb_liegt()
    {
        var model = TimelineBuilder.Build(
            EmptyPlan(),
            new TimelineRange(new IsoWeek(2026, 50), new IsoWeek(2027, 5)),
            today: new DateOnly(2028, 1, 1));

        Assert.Null(model.TodayColumn);
    }
}
