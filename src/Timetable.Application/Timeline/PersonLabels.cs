using Timetable.Domain.Planning;

namespace Timetable.Application.Timeline;

/// <summary>Formatiert Personen-IDs als Kürzel für die Anzeige im Plan.</summary>
internal static class PersonLabels
{
    public static string ShortCode(ProjectPlan plan, Guid? personId) =>
        personId is null ? string.Empty : plan.FindPerson(personId.Value)?.ShortCode ?? "?";

    public static string ShortCodes(ProjectPlan plan, IEnumerable<Guid> personIds) =>
        string.Join(", ", personIds.Select(id => plan.FindPerson(id)?.ShortCode ?? "?"));
}
