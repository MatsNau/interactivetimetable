namespace Timetable.Domain.Calendar;

/// <summary>Ein externes Ereignis als Kontextinformation über der Zeitachse (z. B. "Opernball", "PK 27/28").</summary>
public sealed class ExternalEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Name { get; set; }

    public DateOnly Start { get; set; }

    public DateOnly End { get; set; }

    /// <summary>Hervorhebung in der Anzeige (im Excel gelb markierte Events).</summary>
    public bool IsHighlighted { get; set; }
}
