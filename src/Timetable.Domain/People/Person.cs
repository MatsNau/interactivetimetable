namespace Timetable.Domain.People;

/// <summary>Ein Teammitglied bzw. eine beteiligte Stelle (z. B. "MWK", "DB").</summary>
public sealed class Person
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Kürzel für die kompakte Anzeige im Plan, z. B. "MWK".</summary>
    public required string ShortCode { get; set; }

    public string DisplayName { get; set; } = string.Empty;
}
