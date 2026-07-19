using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Timetable.Domain.Time;

namespace Timetable.Infrastructure.Persistence;

/// <summary>
/// Serialisiert <see cref="FuzzyDate"/> als kompakten, lesbaren String:
/// tagesgenau "2027-06-22", wochengenau "2026-W53", monatsgenau "2027-03".
/// </summary>
public sealed class FuzzyDateJsonConverter : JsonConverter<FuzzyDate>
{
    public override FuzzyDate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value is null
            ? throw new JsonException("FuzzyDate darf nicht null sein.")
            : Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, FuzzyDate value, JsonSerializerOptions options) =>
        writer.WriteStringValue(Format(value));

    internal static string Format(FuzzyDate value)
    {
        switch (value.Precision)
        {
            case FuzzyDatePrecision.Day:
                return value.Anchor.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            case FuzzyDatePrecision.Week:
                var week = IsoWeek.FromDate(value.Anchor);
                return $"{week.Year:D4}-W{week.Week:D2}";
            case FuzzyDatePrecision.Month:
                return value.Anchor.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            default:
                throw new JsonException($"Unbekannte FuzzyDate-Präzision: {value.Precision}.");
        }
    }

    internal static FuzzyDate Parse(string value)
    {
        try
        {
            var weekMarker = value.IndexOf("-W", StringComparison.Ordinal);
            if (weekMarker > 0
                && int.TryParse(value[..weekMarker], NumberStyles.None, CultureInfo.InvariantCulture, out var isoYear)
                && int.TryParse(value[(weekMarker + 2)..], NumberStyles.None, CultureInfo.InvariantCulture, out var weekNumber))
            {
                return FuzzyDate.FromWeek(isoYear, weekNumber);
            }

            if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
                return FuzzyDate.FromDay(day);

            if (DateTime.TryParseExact(value, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var month))
                return FuzzyDate.FromMonth(month.Year, month.Month);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new JsonException($"Ungültiges FuzzyDate \"{value}\": {ex.Message}", ex);
        }

        throw new JsonException($"Ungültiges FuzzyDate-Format: \"{value}\".");
    }
}
