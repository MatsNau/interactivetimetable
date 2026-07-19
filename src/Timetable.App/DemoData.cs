using Timetable.Domain.Calendar;
using Timetable.Domain.People;
using Timetable.Domain.Planning;
using Timetable.Domain.Time;

namespace Timetable.App;

/// <summary>
/// Beispielplan in Anlehnung an die Excel-Vorlage, bis Laden/Speichern
/// über die Oberfläche angebunden ist.
/// </summary>
internal static class DemoData
{
    public static ProjectPlan CreatePlan()
    {
        var mwk = new Person { ShortCode = "MWK", DisplayName = "Ministerium WK" };
        var mf = new Person { ShortCode = "MF", DisplayName = "M. Fischer" };
        var db = new Person { ShortCode = "DB", DisplayName = "D. Berger" };
        var mr = new Person { ShortCode = "MR", DisplayName = "M. Richter" };
        var ar = new Person { ShortCode = "AR", DisplayName = "A. Roth" };

        return new ProjectPlan
        {
            Title = "Projektzeitplan: NSH Organisationsuntersuchung (Demo)",
            AsOfDate = new DateOnly(2026, 6, 16),
            People = [mwk, mf, db, mr, ar],
            Projects =
            [
                new Project
                {
                    Name = "Übergeordnetes Projekt A",
                    LeadId = mwk.Id,
                    ParticipantIds = [db.Id, mf.Id],
                    Milestones =
                    [
                        new Milestone
                        {
                            Title = "Kick-off",
                            PlannedDate = FuzzyDate.FromDay(new DateOnly(2026, 6, 22)),
                            Status = MilestoneStatus.Done,
                            LeadId = mwk.Id,
                            ParticipantIds = [db.Id, mf.Id],
                            Tasks =
                            [
                                new TaskItem
                                {
                                    Title = "Agenda und Einladungen",
                                    Status = MilestoneStatus.Done,
                                    Start = new DateOnly(2026, 6, 15),
                                    End = new DateOnly(2026, 6, 19),
                                    AssigneeIds = [mf.Id],
                                },
                            ],
                        },
                        new Milestone
                        {
                            Title = "Zwischenbericht Gremien",
                            PlannedDate = FuzzyDate.FromDay(new DateOnly(2026, 9, 23)),
                            Status = MilestoneStatus.InProgress,
                            LeadId = mwk.Id,
                            ParticipantIds = [db.Id, mr.Id],
                            Tasks =
                            [
                                new TaskItem
                                {
                                    Title = "Datenauswertung",
                                    Status = MilestoneStatus.InProgress,
                                    Start = new DateOnly(2026, 7, 13),
                                    End = new DateOnly(2026, 7, 24),
                                    AssigneeIds = [db.Id],
                                },
                                new TaskItem
                                {
                                    Title = "Berichtsentwurf",
                                    Start = new DateOnly(2026, 7, 20),
                                    End = new DateOnly(2026, 8, 7),
                                    AssigneeIds = [mf.Id, db.Id],
                                },
                                new TaskItem
                                {
                                    Title = "Abstimmung Ministerium",
                                    Start = new DateOnly(2026, 9, 7),
                                    End = new DateOnly(2026, 9, 11),
                                    AssigneeIds = [mwk.Id],
                                },
                            ],
                        },
                        new Milestone
                        {
                            Title = "Workshop Organisation",
                            PlannedDate = FuzzyDate.FromDay(new DateOnly(2026, 11, 23)),
                            LeadId = mwk.Id,
                            ParticipantIds = [db.Id, mf.Id],
                            Note = "Raum reservieren",
                        },
                        new Milestone
                        {
                            Title = "Abschlussbericht",
                            PlannedDate = FuzzyDate.FromMonth(2027, 3),
                            LeadId = mwk.Id,
                            ParticipantIds = [db.Id, mf.Id],
                        },
                        new Milestone
                        {
                            Title = "Präsentation Ergebnisse",
                            PlannedDate = FuzzyDate.FromMonth(2027, 6),
                            LeadId = mwk.Id,
                        },
                    ],
                },
                new Project
                {
                    Name = "Übergeordnetes Projekt B",
                    LeadId = mf.Id,
                    ParticipantIds = [db.Id, mwk.Id],
                    Milestones =
                    [
                        new Milestone
                        {
                            Title = "Auftaktgespräch",
                            PlannedDate = FuzzyDate.FromDay(new DateOnly(2026, 6, 4)),
                            Status = MilestoneStatus.Done,
                            LeadId = mf.Id,
                            ParticipantIds = [db.Id, mwk.Id],
                        },
                        new Milestone
                        {
                            Title = "Interviews Runde 1",
                            PlannedDate = FuzzyDate.FromWeek(2026, 46),
                            Status = MilestoneStatus.InProgress,
                            LeadId = mf.Id,
                            ParticipantIds = [db.Id],
                            Tasks =
                            [
                                new TaskItem
                                {
                                    Title = "Interviewleitfaden erstellen",
                                    Start = new DateOnly(2026, 7, 16),
                                    End = new DateOnly(2026, 7, 22),
                                    AssigneeIds = [mf.Id, db.Id],
                                },
                                new TaskItem
                                {
                                    Title = "Terminplanung Interviews",
                                    Start = new DateOnly(2026, 10, 26),
                                    End = new DateOnly(2026, 11, 6),
                                    AssigneeIds = [db.Id],
                                },
                            ],
                        },
                        new Milestone
                        {
                            Title = "Sounding Board",
                            PlannedDate = FuzzyDate.FromDay(new DateOnly(2026, 12, 18)),
                            Status = MilestoneStatus.Blocked,
                            LeadId = mf.Id,
                            Note = "Terminabstimmung offen",
                        },
                        new Milestone
                        {
                            Title = "Interviews Runde 2",
                            PlannedDate = FuzzyDate.FromMonth(2027, 1),
                            LeadId = mf.Id,
                            ParticipantIds = [db.Id, mwk.Id],
                        },
                    ],
                },
                new Project
                {
                    Name = "Übergeordnetes Projekt C",
                    LeadId = ar.Id,
                    ParticipantIds = [mf.Id, mwk.Id],
                    Milestones =
                    [
                        new Milestone
                        {
                            Title = "Landtagsanhörung",
                            PlannedDate = FuzzyDate.FromMonth(2026, 12),
                            LeadId = ar.Id,
                            ParticipantIds = [mf.Id, mwk.Id],
                        },
                        new Milestone
                        {
                            Title = "Follow-up Bericht",
                            PlannedDate = FuzzyDate.FromMonth(2027, 7),
                            LeadId = ar.Id,
                        },
                    ],
                },
            ],
            Holidays =
            [
                new HolidayPeriod { Name = "Theaterferien 2026", Start = new DateOnly(2026, 7, 6), End = new DateOnly(2026, 8, 16) },
                new HolidayPeriod { Name = "Herbstferien", Start = new DateOnly(2026, 10, 12), End = new DateOnly(2026, 10, 24) },
                new HolidayPeriod { Name = "Weihnachtsferien", Start = new DateOnly(2026, 12, 23), End = new DateOnly(2027, 1, 6) },
                new HolidayPeriod { Name = "Winterferien", Start = new DateOnly(2027, 2, 1), End = new DateOnly(2027, 2, 2) },
                new HolidayPeriod { Name = "Osterferien", Start = new DateOnly(2027, 3, 22), End = new DateOnly(2027, 4, 3) },
                new HolidayPeriod { Name = "Pfingstferien", Start = new DateOnly(2027, 5, 7), End = new DateOnly(2027, 5, 18) },
                new HolidayPeriod { Name = "Theaterferien 2027", Start = new DateOnly(2027, 7, 5), End = new DateOnly(2027, 8, 15) },
            ],
            ExternalEvents =
            [
                new ExternalEvent { Name = "RealDance", Start = new DateOnly(2027, 1, 25), End = new DateOnly(2027, 1, 29) },
                new ExternalEvent { Name = "Opernball", Start = new DateOnly(2027, 2, 20), End = new DateOnly(2027, 2, 21) },
                new ExternalEvent { Name = "PK 27/28", Start = new DateOnly(2027, 5, 4), End = new DateOnly(2027, 5, 4), IsHighlighted = true },
                new ExternalEvent { Name = "Theaterformen", Start = new DateOnly(2027, 6, 10), End = new DateOnly(2027, 6, 20) },
            ],
        };
    }
}
