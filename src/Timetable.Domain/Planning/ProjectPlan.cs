using Timetable.Domain.Calendar;
using Timetable.Domain.People;

namespace Timetable.Domain.Planning;

/// <summary>
/// Die Wurzel des Datenmodells: der gesamte Projektzeitplan,
/// wie er als eine gemeinsame JSON-Datei gespeichert wird.
/// </summary>
public sealed class ProjectPlan
{
    public required string Title { get; set; }

    /// <summary>"Stand"-Datum aus der Kopfzeile des Plans.</summary>
    public DateOnly AsOfDate { get; set; }

    public List<Project> Projects { get; set; } = [];

    public List<Person> People { get; set; } = [];

    public List<HolidayPeriod> Holidays { get; set; } = [];

    public List<ExternalEvent> ExternalEvents { get; set; } = [];

    public Person? FindPerson(Guid id) => People.FirstOrDefault(p => p.Id == id);
}
