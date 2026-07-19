using System.Text.Json;
using System.Text.Json.Serialization;

namespace Timetable.Infrastructure.Persistence;

/// <summary>Zentrale Serialisierungseinstellungen für die Plandatei.</summary>
public static class PlanJson
{
    /// <summary>
    /// Eingerückt und mit sprechenden Enum-/Datumswerten, damit die Datei
    /// auf dem Share auch für Menschen lesbar bleibt.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter(),
            new FuzzyDateJsonConverter(),
        },
    };
}
