namespace Timetable.Domain.Calendar;

/// <summary>Ein Ferien-/Sperrzeitraum, der als Band hinter der Zeitachse liegt (z. B. "Herbstferien 12.10.–24.10.").</summary>
public sealed class HolidayPeriod
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Name { get; set; }

    public DateOnly Start { get; set; }

    public DateOnly End { get; set; }
}
